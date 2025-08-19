using Microsoft.EntityFrameworkCore;
using Sardanapal.ViewModel.Response;
using Sardanapal.Contract.IRepository;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Services.Statics;

namespace Sardanapal.Identity.Services.Services.UserManager;

public class UserManager<TRepository, TUserKey, TUser, TUR, TUC>
    : IUserManager<TUserKey, TUser>
    where TRepository : IUserRepository<TUserKey, byte, TUser, TUR>
        , IEFRepository<TUserKey, TUser>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUC : class, IUserClaim<TUserKey, byte>, new()
{
    protected virtual string ServiceName => "UserManager";

    protected readonly TRepository _repository;
    protected readonly ITokenService _tokenService;

    public UserManager(TRepository repository, ITokenService tokenService)
    {
        _repository = repository;
        _tokenService = tokenService;
    }

    public virtual async Task<IResponse<TUser>> GetUser(string? email = null, long? phoneNumber = null)
    {
        IResponse<TUser> result = new Response<TUser>(ServiceName, OperationType.Fetch);

        await result.FillAsync(async () =>
        {
            TUser? user;
            if (!string.IsNullOrWhiteSpace(email))
            {
                user = await _repository.FetchAll()
                    .AsQueryable()
                    .AsNoTracking()
                    .Where(x => x.Email == email || x.Username == email)
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    result.Set(StatusCode.Succeeded, user);
                }
                else
                {
                    result.Set(StatusCode.NotExists);
                }

            }
            else if (phoneNumber.HasValue)
            {
                user = await _repository.FetchAll()
                    .AsQueryable().AsNoTracking()
                    .Where(x => x.PhoneNumber == phoneNumber)
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    result.Set(StatusCode.Succeeded, user);
                }
                else
                {
                    result.Set(StatusCode.NotExists);
                }
            }
            else
            {
                throw new ArgumentNullException($"Parameter {nameof(email)} | {nameof(phoneNumber)} is null");
            }
        });

        return result;
    }

    public virtual async Task<IResponse> EditUserData(TUserKey id, string? username = null, string? password = null, long? phonenumber = null, string? email = null, string? firstname = null, string? lastname = null)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Edit);

        await result.FillAsync(async () =>
        {
            var user = await this._repository.FetchAll()
                .AsQueryable().AsNoTracking()
                .Where(x => x.Id.Equals(id)).FirstAsync();

            if (!string.IsNullOrWhiteSpace(username))
                user.Username = username;

            if (!string.IsNullOrWhiteSpace(password))
                user.HashedPassword = password;

            if (phonenumber.HasValue)
                user.PhoneNumber = phonenumber.Value;

            if (!string.IsNullOrWhiteSpace(email))
                user.Email = email;

            if (!string.IsNullOrWhiteSpace(firstname))
                user.FirstName = firstname;

            if (!string.IsNullOrWhiteSpace(lastname))
                user.LastName = lastname;

            await _repository.UpdateAsync(id, user);
            await _repository.SaveChangesAsync();

            result.Set(StatusCode.Succeeded, true);
        });

        return result;
    }

    public virtual async Task<IResponse<string>> Login(string username, string password)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Fetch);

        await result.FillAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentNullException(nameof(username));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));

            var md5Pass = await Utilities.EncryptToMd5(password);

            var user = await _repository.FetchAll()
                .AsQueryable()
                .AsNoTracking()
                .Where(x => x.Username == username
                    && x.HashedPassword == md5Pass)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                result.Set(StatusCode.NotExists);
                return;
            }

            var roles = await _repository.FetchAllUserRoles()
                .AsQueryable()
                .AsNoTracking()
                .Where(x => x.UserId.Equals(user.Id))
                .Select(x => x.RoleId)
                .ToListAsync();

            var tokenRes = _tokenService.GenerateToken(user.Id.ToString()
                , roles?.ToArray() ?? []
                , []);

            if (!tokenRes.IsSuccess)
            {
                tokenRes.ConvertTo<string>(result);
            }
            else
            {
                result.Set(StatusCode.Succeeded, tokenRes.Data);
            }
        });

        return result;
    }

    public virtual async Task<IResponse<bool>> HasRole(byte roleId, TUserKey userKey)
    {
        IResponse<bool> result = new Response<bool>(ServiceName, OperationType.Fetch);
        await result.FillAsync(async () =>
        {
            var data = await this._repository.FetchAllUserRoles()
                .AsQueryable().AsNoTracking()
                .Where(x => x.UserId.Equals(userKey) && x.RoleId.Equals(roleId))
                .AnyAsync();

            result.Set(StatusCode.Succeeded, data);
        });

        return result;
    }

    protected virtual TUser CreateNewUser(string username, string hashedPassword)
    {
        return new TUser()
        {
            Username = username,
            HashedPassword = hashedPassword
        };
    }

    public virtual async Task<IResponse<TUserKey>> RegisterUser(string username, string password, byte role)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Add);

        await result.FillAsync(async () =>
        {
            var hashedPass = await Utilities.EncryptToMd5(password);

            var newUserRes = await GetUser(username);
            if (newUserRes.StatusCode == StatusCode.Exception)
            {
                newUserRes.ConvertTo<TUserKey>(result);
            }
            else if (newUserRes.IsSuccess)
            {
                result.Set(StatusCode.Duplicate, newUserRes.Data.Id);
                return;
            }

            using var transaction = await _repository.BeginTransactionAsync();

            try
            {
                var newUser = CreateNewUser(username, hashedPass);

                await _repository.AddAsync(newUser);
                await _repository.SaveChangesAsync();

                var hasRoleRes = await HasRole(role, newUser.Id);
                if (hasRoleRes.IsSuccess && !hasRoleRes.Data)
                {
                    var roleUser = new TUR()
                    {
                        RoleId = role,
                        UserId = newUser.Id
                    };

                    await _repository.AddUserRoleAsync(roleUser);
                    await _repository.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                result.Set(StatusCode.Succeeded, newUser.Id);

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw ex;
            }
        });

        return result;
    }

    public async Task<IResponse<string>> RefreshToken(TUserKey userId)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Fetch);

        await result.FillAsync(async () =>
        {
            var roles = await _repository.FetchAllUserRoles().AsQueryable().AsNoTracking()
                .Where(x => x.UserId.Equals(userId))
                .Select(x => x.RoleId)
                .ToArrayAsync();

            if (roles != null && roles.Any())
            {
                var resultModel = _tokenService.GenerateToken(userId.ToString(), roles, []);

                if (!resultModel.IsSuccess)
                {
                    result = resultModel;
                }
                else
                {
                    result.Set(StatusCode.Succeeded, resultModel.Data);
                }
            }
            else
            {
                result.Set(StatusCode.NotExists);
            }
        });

        return result;
    }
}
