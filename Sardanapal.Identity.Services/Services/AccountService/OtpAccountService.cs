
using Microsoft.Extensions.Logging;
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.Identity.Domain.Model;

namespace Sardanapal.Identity.Services.Services.AccountService;

public abstract class OtpAccountServiceBase<TOtpUserManager, TRoleManager, TUserKey, TUser, TRole, TUR, TSearchVM, TUserVM, TRegisterVM, TUserEditable>
    : AccountServiceBase<TOtpUserManager, TRoleManager, TUserKey, TUser, TRole, TUR, TSearchVM, TUserVM, TRegisterVM, TUserEditable>
    , IOtpAccountService<TUserKey, TRegisterVM, TUserEditable>
    where TOtpUserManager : class, IUserManager<TUserKey, TUser, TSearchVM, TUserVM, TRegisterVM, TUserEditable>
    where TRoleManager : IRoleManager<TUserKey, byte, TRole, TUR>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRoleBase<byte>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUserVM : UserVM<TUserKey>, new()
    where TSearchVM : UserSearchVM, new()
    where TRegisterVM : RegisterVM<byte>, new()
    where TUserEditable : UserEditableVM, new()
{
    protected override string ServiceName => "OTP AccountService";

    protected readonly IOtpService<TUserKey, Guid, OtpVM<TUserKey>, NewOtpVM<TUserKey>, OtpEditableVM<TUserKey>> _otpService;

    public OtpAccountServiceBase(TOtpUserManager _userManager
        , TRoleManager roleManager
        , IOtpService<TUserKey, Guid, OtpVM<TUserKey>, NewOtpVM<TUserKey>, OtpEditableVM<TUserKey>> otpService
        , ILogger logger)
        : base(_userManager, roleManager, logger)
    {
        _otpService = otpService;
    }

    public virtual async Task<IResponse<TUserKey>> RequestLoginOtp(OtpRequestVM model)
    {
        var result = new Response<TUserKey>(ServiceName, OperationType.Fetch, _logger);

        return await result.FillAsync(async () =>
        {
            dynamic identifier = model.PhoneNumber.HasValue ? model.PhoneNumber
                : !string.IsNullOrWhiteSpace(model.Email) ? model.Email : null;

            if (identifier != null)
            {
                IResponse<TUser> userRes = await _userManager.GetUser(identifier);

                if (userRes.IsSuccess)
                {
                    await _otpService.Add(new NewOtpVM<TUserKey>()
                    {
                        Recipient = identifier,
                        Username = userRes.Data.Username,
                        UserId = userRes.Data.Id,
                        RoleId = model.Role
                    });
                }
                else
                {
                    userRes.ConvertTo<TUserKey>(result);
                }
            }
            else
            {
                result.Set(StatusCode.Canceled);
                result.DeveloperMessages = [Identity_Messages.InvalidEmailOrNumber];
            }
        });
    }

    public virtual async Task<IResponse<LoginDto>> LoginWithOtp(OTPResponseVM<TUserKey> model)
    {
        IResponse<LoginDto> result = new Response<LoginDto>(ServiceName, OperationType.Fetch, _logger);

        return await result.FillAsync(async () =>
        {
            var validateRes = await _otpService.ValidateCode(new NewOtpVM<TUserKey>()
            {
                UserId = model.UserId,
                RoleId = model.RoleId,
                Code = model.Code
            });

            if (validateRes.IsSuccess)
            {
                var loginRes = await _userManager.Login(model.UserId);
                if (loginRes.IsSuccess)
                {

                    result.Set(StatusCode.Succeeded, new LoginDto() { Token = loginRes.Data });
                }
                else
                {
                    loginRes.ConvertTo<LoginDto>(result);
                }
            }
            else
            {
                validateRes.ConvertTo<LoginDto>(result);
            }
        });
    }

    public virtual async Task<IResponse<TUserKey>> RequestRegisterOtp(OtpRequestVM model)
    {
        var result = new Response<TUserKey>(ServiceName, OperationType.Add, _logger);
        return await result.FillAsync(async () =>
        {
            dynamic identifier = model.PhoneNumber.HasValue ? model.PhoneNumber
                : !string.IsNullOrWhiteSpace(model.Email) ? model.Email : null;

            if (identifier != null)
            {
                IResponse<TUser> userRes = await _userManager.GetUser(identifier);

                if (userRes.IsSuccess)
                {
                    await _otpService.Add(new NewOtpVM<TUserKey>()
                    {
                        Recipient = identifier,
                        Username = userRes.Data.Username,
                        UserId = userRes.Data.Id,
                        RoleId = model.Role
                    });
                }
                else
                {
                    userRes.ConvertTo<TUserKey>(result);
                }
            }
            else
            {
                result.Set(StatusCode.Canceled);
                result.DeveloperMessages = [Identity_Messages.InvalidEmailOrNumber];
            }
        });
    }

    public virtual async Task<IResponse> RegisterWithOtp(OTPResponseVM<TUserKey> model)
    {
        var result = new Response(ServiceName, OperationType.Add, _logger);

        await result.FillAsync(async () =>
        {
            var validateRes = await _otpService.ValidateCode(new NewOtpVM<TUserKey>()
            {
                Code = model.Code,
                UserId = model.UserId,
                RoleId = model.RoleId
            });

            if (validateRes.IsSuccess)
            {
                IResponse<TUser> userRes = await _userManager.GetUser(model.UserId);
                if (userRes.IsSuccess)
                {
                    var verifyRes = await _userManager.VerifyUser(validateRes.Data.Recipient);
                    if (verifyRes.IsSuccess)
                    {
                        result.Set(StatusCode.Succeeded, true);
                    }
                    else
                    {
                        verifyRes.ConvertTo<bool>(result);
                    }
                }
                else
                {
                    userRes.ConvertTo<bool>(result);
                }
            }
            else
            {
                validateRes.ConvertTo<bool>(result);
            }
        });

        return result;
    }

    public virtual async Task<IResponse<TUserKey>> RequestResetPassword(ResetPasswordRequestVM model)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            dynamic identifier = model.PhoneNumber.HasValue ? model.PhoneNumber
                : !string.IsNullOrWhiteSpace(model.Email) ? model.Email : null;
            if (identifier != null)
            {
                var userRes = await _userManager.GetUser(identifier);
                if (!userRes.IsSuccess)
                {
                    var otpRes = await _otpService.Add(new NewOtpVM<TUserKey>()
                    {
                        Recipient = identifier,
                        Username = userRes.Data.Username,
                        UserId = userRes.Data.Id
                    });
                    if (otpRes.IsSuccess)
                    {
                        result.Set(StatusCode.Succeeded, userRes.Data.Id);
                    }
                    else
                    {
                        otpRes.ConvertTo<TUserKey>(result);
                    }
                }
                else
                {
                    userRes.ConvertTo<TUserKey>(result);
                }
            }
            else
            {
                result.Set(StatusCode.Canceled, Identity_Messages.InvalidEmailOrNumber);
            }
        });

        return result;
    }

    public virtual async Task<IResponse> ResetPassword(ResetPasswordVM<TUserKey> model)
    {
        var result = new Response(ServiceName, OperationType.Add, _logger);

        await result.FillAsync(async () =>
        {
            var validateRes = await _otpService.ValidateCode(new NewOtpVM<TUserKey>()
            {
                Code = model.Code,
                UserId = model.UserId,
                RoleId = model.RoleId
            });

            if (validateRes.IsSuccess)
            {
                IResponse<TUser> userRes = await _userManager.GetUser(model.UserId);
                if (userRes.IsSuccess)
                {
                    var resetRes = await _userManager.ChangePassword(model.UserId, model.NewPassword);
                    if (resetRes.IsSuccess)
                    {
                        result.Set(StatusCode.Succeeded, true);
                    }
                    else
                    {
                        resetRes.ConvertTo<bool>(result);
                    }
                }
                else
                {
                    userRes.ConvertTo<bool>(result);
                }
            }
            else
            {
                validateRes.ConvertTo<bool>(result);
            }
        });

        return result;
    }
}
