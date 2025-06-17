using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Domain.Data;
using Sardanapal.Identity.Services.Statics;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.UserManager;

public class UserManager<TUserKey, TUser, TRole, TClaim, TUR, TUC> : IUserManager<TUserKey, TUser, TRole, TClaim>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRole<byte>, new()
    where TClaim : class, IClaim<byte>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUC : class, IUserClaim<TUserKey, byte>, new()
{
    protected virtual string ServiceName => "UserManager";

    protected SdIdentityUnitOfWorkBase<TUserKey, byte, byte, TUser, TRole, TClaim, TUR, TUC> _context;
    protected ITokenService _tokenService;

    public DbSet<TUser> Users
    {
        get
        {
            return _context.Set<TUser>();
        }
    }

    public DbSet<TRole> Roles
    {
        get
        {
            return _context.Set<TRole>();
        }
    }

    public UserManager(SdIdentityUnitOfWorkBase<TUserKey, byte, byte, TUser, TRole, TClaim, TUR, TUC> context, ITokenService tokenService)
    {
        _context = context;
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
                user = await Users.AsNoTracking()
                   .Where(x => x.Email == email)
                   .FirstOrDefaultAsync();
                result.Set(StatusCode.Succeeded, user);
            }
            else if (phoneNumber.HasValue)
            {
                user = await Users.AsNoTracking()
                    .Where(x => x.PhoneNumber == phoneNumber)
                    .FirstOrDefaultAsync();
                result.Set(StatusCode.Succeeded, user);
            }
            else
            {
                result.Set(StatusCode.NotExists, null);
            }
        });

        return result;
    }

    public virtual async Task<IResponse> EditUserData(TUserKey id, string? username = null, string? password = null, long? phonenumber = null, string? email = null, string? firstname = null, string? lastname = null)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Edit);

        await result.FillAsync(async () =>
        {
            var user = await _context.Users.Where(x => x.Id.Equals(id)).FirstAsync();

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

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            result.Set(StatusCode.Succeeded, true);
        });

        return result;
    }

    public virtual async Task<IResponse<string>> Login(string username, string password)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Fetch);

        await result.FillAsync(async () =>
        {
            var md5Pass = await Utilities.EncryptToMd5(password);

            var user = await _context.Users.Where(x => x.Username == username
                && x.HashedPassword == md5Pass)
                .FirstAsync();

            var roles = await _context.UserRoles.AsNoTracking()
                .Where(x => x.UserId.Equals(user.Id))
                .Select(x => x.RoleId)
                .ToListAsync();

            var tokenRes = _tokenService.GenerateToken(user.Id.ToString(), roles.ToArray(), []);
            if (!tokenRes.IsSuccess)
            {
                result = tokenRes.ConvertTo<string>();
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
            var data = await this._context.UserRoles.AsNoTracking()
                .Where(x => x.UserId.Equals(userKey) && x.RoleId.Equals(roleId))
                .AnyAsync();

            result.Set(StatusCode.Succeeded, data);
        });

        return result;
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
                result = newUserRes.ConvertTo<TUserKey>();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (newUserRes.Data == null)
                {
                    var newUser = new TUser()
                    {
                        Username = username,
                        HashedPassword = hashedPass
                    };

                    await _context.AddAsync(newUser);
                    await _context.SaveChangesAsync();
                }
                var hasRoleRes = await HasRole(role, newUserRes.Data.Id);
                if (hasRoleRes.IsSuccess && hasRoleRes.Data)
                {
                    var roleUser = new TUR()
                    {
                        RoleId = role,
                        UserId = newUserRes.Data.Id
                    };

                    await _context.AddAsync(roleUser);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                result.Set(StatusCode.Succeeded, newUserRes.Data.Id);

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
            var roles = await _context.UserRoles.AsNoTracking()
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