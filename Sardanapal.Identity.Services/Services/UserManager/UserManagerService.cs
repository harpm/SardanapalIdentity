
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Sardanapal.Service;
using Sardanapal.Contract.IRepository;
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.Share.Static;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.Share.Statics;
using Sardanapal.Contract.Data;

namespace Sardanapal.Identity.Services.Services.UserManager;

#region EF

public class EFUserManager<TEFDatabaseManager, TRepository, TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM, TUR, TUC>
    : EFPanelServiceBase<TEFDatabaseManager, TRepository, TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM>
    , IUserManager<TUserKey, TUser, TRegisterVM>
    where TEFDatabaseManager : IEFDatabaseManager
    where TRepository : IEFUserRepository<TUserKey, byte, TUser, TUR>
        , IEFCrudRepository<TUserKey, TUser>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUC : class, IUserClaim<TUserKey, byte>, new()
    where TUserVM : UserVM<TUserKey>, new()
    where TUserSearchVM : UserSearchVM, new()
    where TRegisterVM : RegisterVM<byte>, new()
    where TUserEditableVM : UserEditableVM, new()
{
    protected override string ServiceName => "UserManager";

    protected readonly ITokenService _tokenService;

    protected override IQueryable<TUser> Search(IQueryable<TUser> entities, TUserSearchVM searchVM)
    {
        if (searchVM == null)
        {
            if (!string.IsNullOrWhiteSpace(searchVM.Username))
            {
                entities.Where(u => u.Username.Contains(searchVM.Username));
            }

            if (!string.IsNullOrWhiteSpace(searchVM.Email))
            {
                entities.Where(u => u.Username.Contains(searchVM.Email));
            }

            if (searchVM.PhoneNumber.HasValue)
            {
                entities.Where(u => u.Username.Contains(searchVM.PhoneNumber.ToString()));
            }
        }

        return entities;
    }

    public EFUserManager(TEFDatabaseManager dbManager
        , TRepository repository
        , IMapper mapper
        , ILogger logger
        , ITokenService tokenService)
        : base(dbManager, repository, mapper, logger)
    {
        _tokenService = tokenService;
    }

    public virtual async Task<IResponse<TUser>> GetUser(string username)
    {
        IResponse<TUser> result = new Response<TUser>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                TUser? user = await _repository.FetchAll()
                        .AsNoTracking()
                        .Where(x => x.Email == username || x.Username == username || x.PhoneNumber.ToString() == username)
                        .FirstOrDefaultAsync();

                if (user != null)
                {
                    result.Set(StatusCode.Succeeded, user);
                }
                else
                {
                    result.Set(StatusCode.NotExists, [], Identity_Messages.UserNotFound);
                }

            }
            else
            {
                throw new ArgumentNullException(
                    StringResourceHelper.CreateNullReferenceEmailOrPhoneNumber("Email", "Phone Number"));
            }
        });

        return result;
    }

    public virtual async Task<IResponse<string>> Login(string username, string password)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentNullException(nameof(username));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));

            var md5Pass = await Utilities.EncryptToMd5(password);

            var user = await _repository.FetchAll()
                .AsNoTracking()
                .Where(x => x.Username == username
                    && x.HashedPassword == md5Pass)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                result.Set(StatusCode.NotExists, [], Identity_Messages.UserNotFound);
                return;
            }

            var roles = await _repository.FetchAllUserRoles()
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

    public virtual async Task<IResponse<bool>> HasRole(TUserKey userKey, byte roleId)
    {
        IResponse<bool> result = new Response<bool>(ServiceName, OperationType.Fetch, _logger);
        await result.FillAsync(async () =>
        {
            var data = await this._repository.FetchAllUserRoles()
                .AsNoTracking()
                .Where(x => x.UserId.Equals(userKey) && x.RoleId.Equals(roleId))
                .AnyAsync();

            result.Set(StatusCode.Succeeded, data);
        });

        return result;
    }

    protected virtual async Task<TUser> CreateNewUser(TRegisterVM model)
    {
        var hashedPass = await Utilities.EncryptToMd5(model.Password);

        return new TUser()
        {
            Username = model.Username,
            HashedPassword = hashedPass
        };
    }

    public virtual async Task<IResponse<TUserKey>> RegisterUser(TRegisterVM model)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Add, _logger);

        await result.FillAsync(async () =>
        {

            var newUserRes = await GetUser(model.Username);
            if (newUserRes.StatusCode == StatusCode.Exception)
            {
                newUserRes.ConvertTo<TUserKey>(result);
            }
            else if (newUserRes.IsSuccess)
            {
                result.Set(StatusCode.Duplicate, newUserRes.Data.Id);
                return;
            }

            using var transaction = await _dbManager.CreatTransactionAsync();

            try
            {
                var newUser = await CreateNewUser(model);

                await _repository.AddAsync(newUser);
                await _dbManager.SaveChangesAsync();

                foreach (byte role in model.Roles)
                {
                    var hasRoleRes = await HasRole(newUser.Id, role);
                    if (hasRoleRes.IsSuccess && !hasRoleRes.Data)
                    {
                        var roleUser = new TUR()
                        {
                            RoleId = role,
                            UserId = newUser.Id
                        };

                        await _repository.AddUserRoleAsync(roleUser);
                        await _dbManager.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();

                result.Set(StatusCode.Succeeded, newUser.Id);

            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        return result;
    }

    public async Task<IResponse> ChangePassword(TUserKey userId, string newPassword)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Edit, _logger);
        await result.FillAsync(async () =>
        {
            var user = await _repository.FetchAll()
                .Where(x => x.Id.Equals(userId))
                .FirstOrDefaultAsync();
            if (user != null)
            {
                var hashedPass = await Utilities.EncryptToMd5(newPassword);
                user.HashedPassword = hashedPass;
                await _repository.UpdateAsync(userId, user);
                await _dbManager.SaveChangesAsync();
                result.Set(StatusCode.Succeeded);
            }
            else
            {
                result.Set(StatusCode.NotExists, Identity_Messages.UserNotFound);
            }
        });
        return result;
    }

    public async Task<IResponse<string>> RefreshToken(TUserKey userId)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            var roles = await _repository.FetchAllUserRoles().AsNoTracking()
                .Where(x => x.UserId.Equals(userId))
                .Select(x => x.RoleId)
                .ToArrayAsync();

            if (roles != null && roles.Any())
            {
                var resultModel = _tokenService.GenerateToken(userId.ToString(), roles, []);

                if (resultModel.IsSuccess)
                {
                    result.Set(StatusCode.Succeeded, resultModel.Data);
                }
                else
                {
                    resultModel.ConvertTo<string>(result);
                }
            }
            else
            {
                result.Set(StatusCode.NotExists, Identity_Messages.InvalidUserId);
            }
        });

        return result;
    }
}

#endregion

#region Memory

public class UserManager<TRepository, TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM, TUR, TUC>
    : MemoryPanelServiceBase<TRepository, TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM>
    , IUserManager<TUserKey, TUser, TRegisterVM>
    where TRepository : class, IMemoryRepository<TUserKey, TUser>, IUserRepository<TUserKey, byte, TUser, TUR>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUC : class, IUserClaim<TUserKey, byte>, new()
    where TUserVM : UserVM<TUserKey>, new()
    where TUserSearchVM : UserSearchVM, new()
    where TRegisterVM : RegisterVM<byte>, new()
    where TUserEditableVM : UserEditableVM, new()
{
    protected override string ServiceName => "UserManager";

    protected readonly ITokenService _tokenService;

    public UserManager(TRepository repository, IMapper mapper, ILogger logger, ITokenService tokenService)
        : base(repository, mapper, logger)
    {
        _tokenService = tokenService;
    }

    protected override IEnumerable<TUser> Search(IEnumerable<TUser> entities, TUserSearchVM searchVM)
    {

        if (searchVM != null)
        {
            if (!string.IsNullOrWhiteSpace(searchVM.Username))
            {
                entities = entities.Where(u => u.Username.Contains(searchVM.Username));
            }
        }

        return entities;
    }

    public virtual Task<IResponse<TUser>> GetUser(string username)
    {
        IResponse<TUser> result = new Response<TUser>(ServiceName, OperationType.Fetch, _logger);

        result.Fill(() =>
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                TUser? user = _repository.FetchAll()
                        .Where(x => x.Email == username || x.Username == username || x.PhoneNumber.ToString() == username)
                        .FirstOrDefault();

                if (user != null)
                {
                    result.Set(StatusCode.Succeeded, user);
                }
                else
                {
                    result.Set(StatusCode.NotExists, [], Identity_Messages.UserNotFound);
                }

            }
            else
            {
                throw new ArgumentNullException(
                    StringResourceHelper.CreateNullReferenceEmailOrPhoneNumber("Email", "Phone Number"));
            }
        });

        return Task.FromResult(result);
    }

    public virtual async Task<IResponse<string>> Login(string username, string password)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentNullException(nameof(username));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentNullException(nameof(password));

            var md5Pass = await Utilities.EncryptToMd5(password);

            var user = _repository.FetchAll()
                .Where(x => x.Username == username
                    && x.HashedPassword == md5Pass)
                .FirstOrDefault();

            if (user == null)
            {
                result.Set(StatusCode.NotExists, [], Identity_Messages.UserNotFound);
                return;
            }

            var roles = _repository.FetchAllUserRoles()
                .Where(x => x.UserId.Equals(user.Id))
                .Select(x => x.RoleId)
                .ToList();

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

    public virtual Task<IResponse<bool>> HasRole(TUserKey userKey, byte roleId)
    {
        IResponse<bool> result = new Response<bool>(ServiceName, OperationType.Fetch, _logger);
        result.Fill(() =>
        {
            var data = this._repository.FetchAllUserRoles()
                .Where(x => x.UserId.Equals(userKey) && x.RoleId.Equals(roleId))
                .Any();

            result.Set(StatusCode.Succeeded, data);
        });

        return Task.FromResult(result);
    }

    protected virtual async Task<TUser> CreateNewUser(TRegisterVM model)
    {
        var hashedPass = await Utilities.EncryptToMd5(model.Password);

        return new TUser()
        {
            Username = model.Username,
            HashedPassword = hashedPass
        };
    }

    public virtual async Task<IResponse<TUserKey>> RegisterUser(TRegisterVM model)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Add, _logger);

        await result.FillAsync(async () =>
        {

            var newUserRes = await GetUser(model.Username);
            if (newUserRes.StatusCode == StatusCode.Exception)
            {
                newUserRes.ConvertTo<TUserKey>(result);
            }
            else if (newUserRes.IsSuccess)
            {
                result.Set(StatusCode.Duplicate, newUserRes.Data.Id);
                return;
            }

            // TODO: Need Transaction support in base repository

            try
            {
                var newUser = await CreateNewUser(model);

                await _repository.AddAsync(newUser);

                foreach (byte role in model.Roles)
                {
                    var hasRoleRes = await HasRole(newUser.Id, role);
                    if (hasRoleRes.IsSuccess && !hasRoleRes.Data)
                    {
                        var roleUser = new TUR()
                        {
                            RoleId = role,
                            UserId = newUser.Id
                        };

                        await _repository.AddUserRoleAsync(roleUser);
                    }
                }

                // TODO: Trasaction commit

                result.Set(StatusCode.Succeeded, newUser.Id);

            }
            catch
            {
                // TODO: Transaction rollback
                throw;
            }
        });

        return result;
    }

    public async Task<IResponse> ChangePassword(TUserKey userId, string newPassword)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Edit, _logger);
        await result.FillAsync(async () =>
        {
            var user = _repository.FetchAll()
                .Where(x => x.Id.Equals(userId))
                .FirstOrDefault();
            if (user != null)
            {
                var hashedPass = await Utilities.EncryptToMd5(newPassword);
                user.HashedPassword = hashedPass;
                await _repository.UpdateAsync(userId, user);
                result.Set(StatusCode.Succeeded);
            }
            else
            {
                result.Set(StatusCode.NotExists, Identity_Messages.UserNotFound);
            }
        });
        return result;
    }


    public async Task<IResponse<string>> RefreshToken(TUserKey userId)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            var roles = _repository.FetchAllUserRoles()
                .Where(x => x.UserId.Equals(userId))
                .Select(x => x.RoleId)
                .ToArray();

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
                result.Set(StatusCode.NotExists, Identity_Messages.InvalidUserId);
            }
        });

        return result;
    }
}

#endregion
