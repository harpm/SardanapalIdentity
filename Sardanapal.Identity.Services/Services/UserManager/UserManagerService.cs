
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
    , IUserManager<TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM>
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

    protected virtual async Task<TUser> CreateNewUser(TRegisterVM model)
    {
        var hashedPass = await Utilities.EncryptToMd5(model.Password);

        return new TUser()
        {
            Username = model.Username,
            HashedPassword = hashedPass
        };
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

    public virtual async Task<IResponse<TUser>> GetUser(TUserKey id)
    {
        IResponse<TUser> result = new Response<TUser>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            TUser? user = await _repository.FetchAll()
                    .AsNoTracking()
                    .Where(x => x.Id.Equals(id))
                    .FirstOrDefaultAsync();

            if (user != null)
            {
                result.Set(StatusCode.Succeeded, user);
            }
            else
            {
                result.Set(StatusCode.NotExists, [], Identity_Messages.UserNotFound);
            }

        });

        return result;
    }

    public virtual async Task<IResponse<string>> Login(TUserKey id)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            var user = await _repository.FetchAll()
                .AsNoTracking()
                .Where(x => x.Id.Equals(id))
                .FirstOrDefaultAsync();

            if (user == null)
            {
                result.Set(StatusCode.NotExists, [], Identity_Messages.UserNotFound);
                return;
            }
            else
            {
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
            }
        });

        return result;
    }

    public virtual async Task<IResponse<string>> RefreshToken(TUserKey userId)
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
                    var roleUser = new TUR()
                    {
                        RoleId = role,
                        UserId = newUser.Id
                    };

                    await _repository.AddUserRoleAsync(roleUser);
                    await _dbManager.SaveChangesAsync();
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

    public virtual async Task<IResponse> VerifyUser(string recepient)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Edit, _logger);
        await result.FillAsync(async () =>
        {
            if (!string.IsNullOrWhiteSpace(recepient))
            {
                TUser? user = null;
                if (ulong.TryParse(recepient, out ulong phoneNumber))
                {
                    user = await _repository.FetchAll()
                        .Where(x => x.PhoneNumber.HasValue && x.PhoneNumber.Value == phoneNumber)
                        .FirstOrDefaultAsync();

                    if (user != null)
                    {
                        user.VerifiedPhoneNumber = true;
                    }
                }
                else
                {
                    user = await _repository.FetchAll()
                        .Where(x => x.Email == recepient)
                        .FirstOrDefaultAsync();

                    if (user != null)
                    {
                        user.VerifiedEmail = true;
                    }
                }

                if (user != null)
                {
                    await _repository.UpdateAsync(user.Id, user);
                    await _dbManager.SaveChangesAsync();
                    result.Set(StatusCode.Succeeded);
                }
                else
                {
                    result.Set(StatusCode.NotExists, Identity_Messages.UserNotFound);
                }
            }
            else
            {
                result.Set(StatusCode.Failed, Identity_Messages.InvalidEmailOrNumber);
            }
            
        });
        return result;
    }

    public virtual async Task<IResponse> Edit(TUserKey id, TUserEditableVM model)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Edit, _logger);

        await result.FillAsync(async () =>
        {
            var user = await _repository.FetchByIdAsync(id);
            if (user != null)
            {
                _mapper.Map(model, user);

                await _repository.UpdateAsync(id, user);
                await _dbManager.SaveChangesAsync();

                result.Set(StatusCode.Succeeded, true);
            }
            else
            {
                result.Set(StatusCode.NotExists);
            }
        });

        return result;
    }

    public virtual async Task<IResponse> ChangePassword(TUserKey userId, string newPassword)
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

    public virtual async Task<IResponse> DeleteUser(TUserKey userId)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Delete, _logger);

        await result.FillAsync(async () =>
        {
            if (await _repository.FetchAll().AsNoTracking().Where(x => x.Id.Equals(userId)).AnyAsync())
            {
                await _repository.DeleteAsync(userId);
                await _dbManager.SaveChangesAsync();

                result.Set(StatusCode.Succeeded, true);
            }
            else
            {
                result.Set(StatusCode.NotExists, false);
            }
        });

        return result;
    }
}

#endregion

#region Memory

public class UserManager<TRepository, TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM, TUR, TUC>
    : MemoryPanelServiceBase<TRepository, TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM>
    , IUserManager<TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM>
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

    protected virtual async Task<TUser> CreateNewUser(TRegisterVM model)
    {
        var hashedPass = await Utilities.EncryptToMd5(model.Password);

        return new TUser()
        {
            Username = model.Username,
            HashedPassword = hashedPass
        };
    }

    public virtual async Task<IResponse<TUser>> GetUser(string username)
    {
        IResponse<TUser> result = new Response<TUser>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
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

        return result;
    }

    public virtual async Task<IResponse<TUser>> GetUser(TUserKey id)
    {
        IResponse<TUser> result = new Response<TUser>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            TUser? user = _repository.FetchAll()
                    .Where(x => x.Id.Equals(id))
                    .FirstOrDefault();

            if (user != null)
            {
                result.Set(StatusCode.Succeeded, user);
            }
            else
            {
                result.Set(StatusCode.NotExists, [], Identity_Messages.UserNotFound);
            }

        });

        return result;
    }

    public virtual async Task<IResponse<string>> Login(TUserKey id)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            var user = _repository.FetchAll()
                .Where(x => x.Id.Equals(id))
                .FirstOrDefault();

            if (user == null)
            {
                result.Set(StatusCode.NotExists, [], Identity_Messages.UserNotFound);
                return;
            }
            else
            {
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
            }
        });

        return result;
    }

    public virtual async Task<IResponse<string>> RefreshToken(TUserKey userId)
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

            var newUser = await CreateNewUser(model);

            await _repository.AddAsync(newUser);

            foreach (byte role in model.Roles)
            {
                var roleUser = new TUR()
                {
                    RoleId = role,
                    UserId = newUser.Id
                };

                await _repository.AddUserRoleAsync(roleUser);
            }


            result.Set(StatusCode.Succeeded, newUser.Id);
        });

        return result;
    }

    public virtual async Task<IResponse> VerifyUser(string recepient)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Edit, _logger);
        await result.FillAsync(async () =>
        {
            if (!string.IsNullOrWhiteSpace(recepient))
            {
                TUser? user = null;
                if (ulong.TryParse(recepient, out ulong phoneNumber))
                {
                    user = _repository.FetchAll()
                        .Where(x => x.PhoneNumber.HasValue && x.PhoneNumber.Value == phoneNumber)
                        .FirstOrDefault();

                    if (user != null)
                    {
                        user.VerifiedPhoneNumber = true;
                    }
                }
                else
                {
                    user = _repository.FetchAll()
                        .Where(x => x.Email == recepient)
                        .FirstOrDefault();

                    if (user != null)
                    {
                        user.VerifiedEmail = true;
                    }
                }

                if (user != null)
                {
                    await _repository.UpdateAsync(user.Id, user);
                    result.Set(StatusCode.Succeeded);
                }
                else
                {
                    result.Set(StatusCode.NotExists, Identity_Messages.UserNotFound);
                }
            }
            else
            {
                result.Set(StatusCode.Failed, Identity_Messages.InvalidEmailOrNumber);
            }

        });
        return result;
    }

    public virtual async Task<IResponse> Edit(TUserKey id, TUserEditableVM model)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Edit, _logger);

        await result.FillAsync(async () =>
        {
            var user = await _repository.FetchByIdAsync(id);
            if (user != null)
            {
                _mapper.Map(model, user);

                await _repository.UpdateAsync(id, user);

                result.Set(StatusCode.Succeeded, true);
            }
            else
            {
                result.Set(StatusCode.NotExists);
            }
        });

        return result;
    }

    public virtual async Task<IResponse> ChangePassword(TUserKey userId, string newPassword)
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

    public virtual async Task<IResponse> DeleteUser(TUserKey userId)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Delete, _logger);

        await result.FillAsync(async () =>
        {
            if (_repository.FetchAll().Where(x => x.Id.Equals(userId)).Any())
            {
                await _repository.DeleteAsync(userId);

                result.Set(StatusCode.Succeeded, true);
            }
            else
            {
                result.Set(StatusCode.NotExists, false);
            }
        });

        return result;
    }
}

#endregion
