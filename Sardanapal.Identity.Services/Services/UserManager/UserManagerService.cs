
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Sardanapal.Service;
using Sardanapal.Ef.Repository;
using Sardanapal.Ef.UnitOfWork;
using Sardanapal.Contract.IRepository;
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.Share.Static;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.Share.Statics;

namespace Sardanapal.Identity.Services.Services.UserManager;

#region EF

public class EFUserManager<TEFDatabaseManager, TRepository, TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM, TUR, TUC, TClaim>
    : EFPanelServiceBase<TEFDatabaseManager, TRepository, TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM>
    , IUserManager<TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM>
    where TEFDatabaseManager : IEFDatabaseManager
    where TRepository : IEFUserRepository<TUserKey, byte, TUser, TUR, TUC, TClaim>
        , IEFCrudRepository<TUserKey, TUser>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUC : class, IUserClaim<TUserKey, byte>, new()
    where TClaim : class, IClaim<byte>, new()
    where TUserVM : UserVM<TUserKey>, new()
    where TUserSearchVM : UserSearchVM, new()
    where TRegisterVM : RegisterVM<byte>, new()
    where TUserEditableVM : UserEditableVM, new()
{
    protected override string ServiceName => "UserManager";

    protected readonly ITokenService _tokenService;

    protected override IQueryable<TUser> Search(IQueryable<TUser> entities, TUserSearchVM searchVM)
    {
        if (searchVM != null)
        {
            if (!string.IsNullOrWhiteSpace(searchVM.Username))
            {
                entities = entities.Where(u => u.Username.Contains(searchVM.Username));
            }

            if (!string.IsNullOrWhiteSpace(searchVM.Email))
            {
                entities = entities.Where(u => u.Email.Contains(searchVM.Email));
            }

            if (searchVM.PhoneNumber.HasValue)
            {
                entities = entities.Where(u => u.PhoneNumber.HasValue && u.PhoneNumber.Value.ToString().Contains(searchVM.PhoneNumber.ToString()));
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

    protected virtual Task<TUser> CreateNewUser(TRegisterVM model)
    {
        var hashedPass = Utilities.HashPassword(model.Password);

        return Task.FromResult(new TUser()
        {
            Username = model.Username,
            HashedPassword = hashedPass
        });
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

                var claimEntities = await _repository.FetchUserClaims(user.Id)
                    .AsNoTracking()
                    .ToListAsync();

                var claims = claimEntities
                    .Select(c => (IClaim)c)
                    .ToArray();

                var tokenRes = _tokenService.GenerateToken(user.Id.ToString()
                    , roles?.ToArray() ?? []
                    , claims ?? []
                    , user.MustChangePassword);

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
            bool userExists = await _repository.FetchAll().AsNoTracking()
                .Where(x => x.Id.Equals(userId))
                .AnyAsync();

            if (!userExists)
            {
                result.Set(StatusCode.NotExists, Identity_Messages.UserNotFound);
                return;
            }

            var roles = await _repository.FetchAllUserRoles().AsNoTracking()
                .Where(x => x.UserId.Equals(userId))
                .Select(x => x.RoleId)
                .ToArrayAsync();

            var claimEntities = await _repository.FetchUserClaims(userId).AsNoTracking()
                .ToListAsync();

            var claims = claimEntities
                .Select(c => (IClaim)c)
                .ToArray();

            var resultModel = _tokenService.GenerateToken(userId.ToString(), roles ?? [], claims ?? []);

            if (resultModel.IsSuccess)
            {
                result.Set(StatusCode.Succeeded, resultModel.Data);
            }
            else
            {
                resultModel.ConvertTo<string>(result);
            }
        });

        return result;
    }

    public virtual async Task<IResponse<TUserKey>> RegisterUser(TRegisterVM model)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Add, _logger);

        await result.FillAsync(async () =>
        {

            var existingUser = await _repository.FetchAll().AsNoTracking()
                .Where(x => x.Username == model.Username)
                .AnyAsync();

            if (existingUser)
            {
                result.Set(StatusCode.Duplicate, Identity_Messages.DuplicateUsername);
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

                var currentRoles = await _repository.FetchAllUserRoles()
                        .AsNoTracking()
                        .Where(r => r.UserId.Equals(id))
                        .Select(r => r.RoleId)
                        .ToArrayAsync();

                await _repository.DeleteUserRolesAsync(id,
                    currentRoles.Where(r => !model.Roles.Contains(r)).ToArray());

                foreach (var role in model.Roles.Where(r => !currentRoles.Contains(r)))
                {
                    var userRole = new TUR()
                    {
                        UserId = id,
                        RoleId = role
                    };
                    await _repository.AddUserRoleAsync(userRole);
                }

                var modelClaims = model.Claims ?? new List<byte>();
                var currentClaims = await _repository.FetchAllUserClaims()
                        .AsNoTracking()
                        .Where(c => c.UserId.Equals(id))
                        .Select(c => c.ClaimId)
                        .ToArrayAsync();

                await _repository.DeleteUserClaimsAsync(id,
                    currentClaims.Where(c => !modelClaims.Contains(c)).ToArray());

                foreach (var claimId in modelClaims.Where(c => !currentClaims.Contains(c)))
                {
                    var userClaim = new TUC()
                    {
                        UserId = id,
                        ClaimId = claimId
                    };
                    await _repository.AddUserClaimAsync(userClaim);
                }

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
                var hashedPass = Utilities.HashPassword(newPassword);
                user.HashedPassword = hashedPass;
                user.MustChangePassword = false;
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

public class UserManager<TRepository, TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM, TUR, TUC, TClaim>
    : MemoryPanelServiceBase<TRepository, TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM>
    , IUserManager<TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditableVM>
    where TRepository : class, IMemoryRepository<TUserKey, TUser>, IUserRepository<TUserKey, byte, TUser, TUR, TUC, TClaim>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUC : class, IUserClaim<TUserKey, byte>, new()
    where TClaim : class, IClaim<byte>, new()
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

            if (!string.IsNullOrWhiteSpace(searchVM.Email))
            {
                entities = entities.Where(u => u.Email != null && u.Email.Contains(searchVM.Email));
            }

            if (searchVM.PhoneNumber.HasValue)
            {
                entities = entities.Where(u => u.PhoneNumber.HasValue && u.PhoneNumber.Value.ToString().Contains(searchVM.PhoneNumber.ToString()));
            }
        }

        return entities;
    }

    protected virtual Task<TUser> CreateNewUser(TRegisterVM model)
    {
        var hashedPass = Utilities.HashPassword(model.Password);

        return Task.FromResult(new TUser()
        {
            Username = model.Username,
            HashedPassword = hashedPass
        });
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

                var claimEntities = _repository.FetchUserClaims(user.Id)
                    .ToList();

                var claims = claimEntities
                    .Select(c => (IClaim)c)
                    .ToArray();

                var tokenRes = _tokenService.GenerateToken(user.Id.ToString()
                    , roles?.ToArray() ?? []
                    , claims ?? []
                    , user.MustChangePassword);

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
            bool userExists = _repository.FetchAll()
                .Where(x => x.Id.Equals(userId))
                .Any();

            if (!userExists)
            {
                result.Set(StatusCode.NotExists, Identity_Messages.UserNotFound);
                return;
            }

            var roles = _repository.FetchAllUserRoles()
                .Where(x => x.UserId.Equals(userId))
                .Select(x => x.RoleId)
                .ToArray();

            var claimEntities = _repository.FetchUserClaims(userId)
                .ToList();

            var claims = claimEntities
                .Select(c => (IClaim)c)
                .ToArray();

            var resultModel = _tokenService.GenerateToken(userId.ToString(), roles ?? [], claims ?? []);

            if (resultModel.IsSuccess)
            {
                result.Set(StatusCode.Succeeded, resultModel.Data);
            }
            else
            {
                resultModel.ConvertTo<string>(result);
            }
        });

        return result;
    }

    public virtual async Task<IResponse<TUserKey>> RegisterUser(TRegisterVM model)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Add, _logger);

        await result.FillAsync(async () =>
        {

            var existingUser = _repository.FetchAll()
                .Where(x => x.Username == model.Username)
                .Any();

            if (existingUser)
            {
                result.Set(StatusCode.Duplicate, Identity_Messages.DuplicateUsername);
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

                var currentRoles = _repository.FetchAllUserRoles()
                        .Where(r => r.UserId.Equals(id))
                        .Select(r => r.RoleId)
                        .ToArray();

                await _repository.DeleteUserRolesAsync(id,
                    currentRoles.Where(r => !model.Roles.Contains(r)).ToArray());

                foreach (var role in model.Roles.Where(r => !currentRoles.Contains(r)))
                {
                    var userRole = new TUR()
                    {
                        UserId = id,
                        RoleId = role
                    };
                    await _repository.AddUserRoleAsync(userRole);
                }

                var modelClaims = model.Claims ?? new List<byte>();
                var currentClaims = _repository.FetchAllUserClaims()
                        .Where(c => c.UserId.Equals(id))
                        .Select(c => c.ClaimId)
                        .ToArray();

                await _repository.DeleteUserClaimsAsync(id,
                    currentClaims.Where(c => !modelClaims.Contains(c)).ToArray());

                foreach (var claimId in modelClaims.Where(c => !currentClaims.Contains(c)))
                {
                    var userClaim = new TUC()
                    {
                        UserId = id,
                        ClaimId = claimId
                    };
                    await _repository.AddUserClaimAsync(userClaim);
                }

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
                var hashedPass = Utilities.HashPassword(newPassword);
                user.HashedPassword = hashedPass;
                user.MustChangePassword = false;
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
