
using AutoMapper;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sardanapal.Contract.IRepository;
using Sardanapal.Service.Repository;
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.Contract.Data;

namespace Sardanapal.Identity.Services.Services.UserManager;

public class EFOtpUserManagerService<TEFDatabaseManager, TRepository, TOtpService, TUserKey, TUser, TUR, TUC, TUserSearchVM, TUserVM, TNewVM, TEditableVM, TOTPLoginVM, TOTPRegisterVM, TOTPRegisterRquestVM>
    : EFUserManager<TEFDatabaseManager, TRepository, TUserKey, TUser, TUserSearchVM, TUserVM, TOTPRegisterVM, TEditableVM, TUR, TUC>
    , IOtpUserManager<TUserKey, TUser, TOTPRegisterVM, TOTPRegisterRquestVM>
    where TEFDatabaseManager : IEFDatabaseManager
    where TRepository : IEFUserRepository<TUserKey, byte, TUser, TUR>, IEFCrudRepository<TUserKey, TUser>
    where TOtpService : class, IOtpServiceBase<TUserKey, Guid, TNewVM, TOTPLoginVM, TOTPRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUC : class, IUserClaim<TUserKey, byte>, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
    where TOTPLoginVM : OTPLoginVM<TUserKey>, new()
    where TOTPRegisterVM : OTPRegisterVM<TUserKey>, new()
    where TUserVM : UserVM<TUserKey>, new()
    where TUserSearchVM : UserSearchVM, new()
    where TOTPRegisterRquestVM : OtpRegisterRequestVM, new()
{
    protected TOtpService OtpService { get; set; }

    public EFOtpUserManagerService(TEFDatabaseManager dbManager
        , TRepository repository
        , IMapper mapper
        , ILogger logger
        , ITokenService tokenService
        , TOtpService _otpService)
        : base(dbManager, repository, mapper, logger, tokenService)

    {
        OtpService = _otpService;
    }

    public virtual async Task<IResponse<TUserKey>> RequestLoginUser(long phonenumber, byte role)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            var user = await _repository
                .FetchAll()
                .AsNoTracking()
                .Where(x => x.PhoneNumber == phonenumber)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                var otpRes = await OtpService.Add(new TNewVM()
                {
                    UserId = user.Id,
                    RoleId = role,
                    Recipient = phonenumber.ToString()
                });

                if (otpRes.StatusCode != StatusCode.Succeeded)
                {
                    throw new Exception(string.Join(", ", otpRes.DeveloperMessages));
                }

                result.Set(StatusCode.Succeeded, user.Id);
            }
            else
            {
                result.Set(StatusCode.NotExists);
            }
        });

        return result;
    }

    public virtual async Task<IResponse<TUserKey>> RequestLoginUser(string email, byte role)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            var user = await _repository
                .FetchAll()
                .AsNoTracking()
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                var otpRes = await OtpService.Add(new TNewVM()
                {
                    UserId = user.Id,
                    RoleId = role,
                    Recipient = email
                });

                if (otpRes.StatusCode != StatusCode.Succeeded)
                {
                    throw new Exception(string.Join(", ", otpRes.DeveloperMessages));
                }

                result.Set(StatusCode.Succeeded, user.Id);
            }
            else
            {
                throw new KeyNotFoundException(Identity_Messages.EmailNotFound);
            }
        });

        return result;
    }

    public virtual async Task<IResponse<TUserKey>> RequestRegisterUser(TOTPRegisterRquestVM model, byte role)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Add, _logger);

        await result.FillAsync(async () =>
        {
            var curUser = await _repository
                .FetchAll()
                .AsNoTracking()
                .Where(x => x.PhoneNumber == model.PhoneNumber || x.Email == model.Email)
                .FirstOrDefaultAsync();

            if (curUser == null)
            {
                curUser = _mapper.Map<TUser>(model);

                await _repository.AddAsync(curUser);
                await _dbManager.SaveChangesAsync();

                result.Set(StatusCode.Succeeded, curUser.Id);
            }
            else if (curUser.VerifiedPhoneNumber)
            {
                throw new DuplicateNameException(Identity_Messages.DuplicatePhoneNumber);
            }

            if (curUser != null && !curUser.VerifiedPhoneNumber)
            {
                await OtpService.Add(new TNewVM()
                {
                    Recipient = model.PhoneNumber.HasValue ? model.PhoneNumber.ToString() : model.Email,
                    UserId = curUser.Id,
                    RoleId = role
                });
            }

            result.Set(StatusCode.Succeeded, curUser.Id);
        });

        return result;
    }

    public virtual async Task<IResponse> VerifyRegisterOtpCode(string code, TUserKey id, byte roleId)
    {
        var result = new Response(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            var curUser = await _repository
                .FetchAll()
                .AsNoTracking()
                .Where(x => x.Id.Equals(id))
                .FirstOrDefaultAsync();

            if (curUser != null)
            {
                var validationRes = await OtpService.ValidateOtpRegister(new TOTPRegisterVM { UserId = id, Code = code, RoleId = roleId });

                if (validationRes.StatusCode == StatusCode.Succeeded && validationRes.Data)
                {
                    if (!string.IsNullOrWhiteSpace(curUser.Email))
                    {
                        curUser.VerifiedEmail = true;
                    }
                    else if (curUser.PhoneNumber.HasValue)
                    {
                        curUser.VerifiedPhoneNumber = true;
                    }
                    else
                    {
                        throw new InvalidOperationException(Identity_Messages.InvalidUserData);
                    }

                    await _repository.UpdateAsync(id, curUser);
                    await _dbManager.SaveChangesAsync();

                    result.Set(StatusCode.Succeeded, true);
                }
                else
                {
                    validationRes.ConvertTo<bool>(result);
                }
            }
            else
            {
                throw new KeyNotFoundException(Identity_Messages.InvalidUserId);
            }
        });

        return result;
    }

    public virtual async Task<IResponse<string>> VerifyLoginOtpCode(string code, TUserKey id, byte roleId)
    {
        var result = new Response<string>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            var curUser = await _repository
                .FetchAll()
                .AsNoTracking()
                .Where(x => x.Id.Equals(id))
                .FirstOrDefaultAsync();

            if (curUser != null)
            {
                var validationRes = await OtpService.ValidateOtpLogin(new TOTPLoginVM
                {
                    UserId = id,
                    Code = code,
                    RoleId = roleId
                });

                if (validationRes.StatusCode == StatusCode.Succeeded && validationRes.Data)
                {
                    var tokenRes = _tokenService.GenerateToken(curUser.Username, [roleId], []);
                    result.Set(StatusCode.Succeeded, tokenRes.StatusCode == StatusCode.Succeeded ? tokenRes.Data : string.Empty);
                }
                else
                {
                    throw new Exception(string.Join(", "
                        , validationRes.DeveloperMessages
                        , "StatusCode: " + validationRes.StatusCode.ToString()));
                }
            }
            else
            {
                throw new Exception(Identity_Messages.InvalidUserId);
            }
        });

        return result;
    }
}

public class OtpUserManagerService<TRepository, TOtpService, TUserKey, TUser, TUR, TUC, TUserSearchVM, TUserVM, TNewVM, TEditableVM, TOTPLoginVM, TOTPRegisterVM, TOTPRegisterRquestVM>
    : UserManager<TRepository, TUserKey, TUser, TUserSearchVM, TUserVM, TOTPRegisterVM, TEditableVM, TUR, TUC>
    , IOtpUserManager<TUserKey, TUser, TOTPRegisterVM, TOTPRegisterRquestVM>
    where TRepository : MemoryRepositoryBase<TUserKey, TUser>, IUserRepository<TUserKey, byte, TUser, TUR>
    where TOtpService : class, IOtpServiceBase<TUserKey, Guid, TNewVM, TOTPLoginVM, TOTPRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUC : class, IUserClaim<TUserKey, byte>, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
    where TOTPLoginVM : OTPLoginVM<TUserKey>, new()
    where TOTPRegisterVM : OTPRegisterVM<TUserKey>, new()
    where TUserVM : UserVM<TUserKey>, new()
    where TUserSearchVM : UserSearchVM, new()
    where TOTPRegisterRquestVM : OtpRegisterRequestVM, new()
{
    protected TOtpService OtpService { get; set; }

    public OtpUserManagerService(TRepository repository
        , IMapper mapper
        , ILogger logger
        , ITokenService tokenService
        , TOtpService _otpService)
        : base(repository, mapper, logger, tokenService)

    {
        OtpService = _otpService;
    }

    public virtual async Task<IResponse<TUserKey>> RequestLoginUser(long phonenumber, byte role)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            var user = _repository
                .FetchAll()
                .Where(x => x.PhoneNumber == phonenumber)
                .FirstOrDefault();

            if (user != null)
            {
                var otpRes = await OtpService.Add(new TNewVM()
                {
                    UserId = user.Id,
                    RoleId = role,
                    Recipient = phonenumber.ToString()
                });

                if (otpRes.StatusCode != StatusCode.Succeeded)
                {
                    throw new Exception(string.Join(", ", otpRes.DeveloperMessages));
                }

                result.Set(StatusCode.Succeeded, user.Id);
            }
            else
            {
                throw new KeyNotFoundException(Identity_Messages.PhonenumberNotFound);
            }
        });
        return result;
    }

    public virtual async Task<IResponse<TUserKey>> RequestLoginUser(string email, byte role)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            var user = _repository
                .FetchAll()
                .Where(x => x.Email == email)
                .FirstOrDefault();

            if (user != null)
            {
                await OtpService.Add(new TNewVM()
                {
                    UserId = user.Id,
                    RoleId = role,
                    Recipient = email
                });

                result.Set(StatusCode.Succeeded, user.Id);
            }
            else
            {
                throw new KeyNotFoundException(Identity_Messages.EmailNotFound);
            }
        });

        return result;
    }

    public virtual async Task<IResponse<TUserKey>> RequestRegisterUser(TOTPRegisterRquestVM model, byte role)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Add, _logger);

        await result.FillAsync(async () =>
        {
            var curUser = _repository
                .FetchAll()
                .Where(x => x.PhoneNumber == model.PhoneNumber || x.Email == model.Email)
                .FirstOrDefault();

            if (curUser == null)
            {
                curUser = _mapper.Map<TUser>(model);

                //curUser = new TUser()
                //{
                //    Username = model.PhoneNumber.HasValue ? model.PhoneNumber.ToString() : model.Email,
                //    PhoneNumber = model.PhoneNumber,
                //    Email = model.Email,
                //    FirstName = model.FirstName,
                //    LastName = model.LastName
                //};

                await _repository.AddAsync(curUser);

                result.Set(StatusCode.Succeeded, curUser.Id);
            }
            else if (curUser.VerifiedPhoneNumber)
            {
                throw new DuplicateNameException(Identity_Messages.DuplicatePhoneNumber);
            }

            if (curUser != null && !curUser.VerifiedPhoneNumber)
            {
                await OtpService.Add(new TNewVM()
                {
                    Recipient = model.PhoneNumber.HasValue ? model.PhoneNumber.ToString() : model.Email,
                    UserId = curUser.Id,
                    RoleId = role
                });
            }

            result.Set(StatusCode.Succeeded, curUser.Id);
        });

        return result;
    }

    public virtual async Task<IResponse> VerifyRegisterOtpCode(string code, TUserKey id, byte roleId)
    {
        Response result = new Response(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            var curUser = _repository
                .FetchAll()
                .Where(x => x.Id.Equals(id))
                .FirstOrDefault();

            if (curUser != null)
            {
                var validationRes = await OtpService.ValidateOtpRegister(new TOTPRegisterVM { UserId = id, Code = code, RoleId = roleId });

                if (validationRes.StatusCode == StatusCode.Succeeded && validationRes.Data)
                {
                    if (!string.IsNullOrWhiteSpace(curUser.Email))
                    {
                        curUser.VerifiedEmail = true;
                    }
                    else if (curUser.PhoneNumber.HasValue)
                    {
                        curUser.VerifiedPhoneNumber = true;
                    }
                    else
                    {
                        throw new InvalidOperationException(Identity_Messages.InvalidUserData);
                    }

                    await _repository.UpdateAsync(id, curUser);

                    result.Set(StatusCode.Succeeded, true);
                }
                else
                {
                    validationRes.ConvertTo<bool>(result);
                }
            }
            else
            {
                throw new KeyNotFoundException(Identity_Messages.InvalidUserId);
            }
        });

        return result;
    }

    public virtual async Task<IResponse<string>> VerifyLoginOtpCode(string code, TUserKey id, byte roleId)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            var curUser = _repository
                .FetchAll()
                .Where(x => x.Id.Equals(id))
                .FirstOrDefault();

            if (curUser != null)
            {
                var validationRes = await OtpService.ValidateOtpLogin(new TOTPLoginVM
                {
                    UserId = id,
                    Code = code,
                    RoleId = roleId
                });

                if (validationRes.StatusCode == StatusCode.Succeeded && validationRes.Data)
                {
                    var tokenRes = _tokenService.GenerateToken(curUser.Username, [roleId], []);

                    if (tokenRes.IsSuccess)
                    {
                        result.Set(StatusCode.Succeeded, tokenRes.Data);
                    }
                    else
                    {
                        tokenRes.ConvertTo<string>(result);
                    }
                }
                else
                {
                    validationRes.ConvertTo<string>(result);
                }
            }
            else
            {
                throw new Exception(Identity_Messages.InvalidUserId);
            }
        });

        return result;
    }
}
