
using Microsoft.Extensions.Logging;
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Otp;

namespace Sardanapal.Identity.Services.Services.AccountService;

public abstract class OtpAccountServiceBase<TOtpUserManager, TUserKey, TUser, TUR, TLoginVM, TLoginDto, TRegisterVM, TOTPLoginRequestVM, TOTPLoginVM, TOTPRegisterRequestVM, TOTPRegisterVM>
    : AccountServiceBase<TOtpUserManager, TUserKey, TUser, TLoginVM, TLoginDto, TRegisterVM>
    , IOtpAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM, TOTPLoginRequestVM, TOTPLoginVM, TOTPRegisterRequestVM, TOTPRegisterVM>
    where TOtpUserManager : class, IOtpUserManager<TUserKey, TUser, TRegisterVM, TOTPRegisterRequestVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TLoginVM : LoginVM, new()
    where TLoginDto : LoginDto, new()
    where TRegisterVM : RegisterVM, new()
    where TOTPLoginRequestVM : OtpLoginRequestVM, new()
    where TOTPRegisterRequestVM : OtpRegisterRequestVM, new()
    where TOTPLoginVM : OTPLoginVM<TUserKey>, new()
    where TOTPRegisterVM : OTPRegisterVM<TUserKey>, new()
{
    protected override string ServiceName => "OTP AccountService";
    public OtpAccountServiceBase(TOtpUserManager _userManagerService, ILogger logger)
        : base(_userManagerService, logger)
    {

    }

    public virtual async Task<IResponse<TUserKey>> RequestLoginOtp(TOTPLoginRequestVM model)
    {
        var result = new Response<TUserKey>(ServiceName, OperationType.Fetch, _logger);

        return await result.FillAsync(async () =>
        {
            dynamic identifier = model.PhoneNumber.HasValue ? model.PhoneNumber
                : !string.IsNullOrWhiteSpace(model.Email) ? model.Email : null;

            if (identifier != null)
            {
                var uid = await userManagerService
                    .RequestLoginUser(identifier, roleId);
                result.Set(StatusCode.Succeeded, uid);
            }
            else
            {
                result.Set(StatusCode.Canceled);
                result.DeveloperMessages = [Identity_Messages.InvalidEmailOrNumber];
            }
        });
    }

    public virtual async Task<IResponse<TLoginDto>> LoginWithOtp(TOTPLoginVM Model)
    {
        var result = new Response<TLoginDto>(ServiceName, OperationType.Fetch, _logger);

        return await result.FillAsync(async () =>
        {
            var tokenRes = await userManagerService.VerifyLoginOtpCode(Model.Code, Model.UserId, Model.RoleId);

            if (tokenRes.IsSuccess)
            {
                result.Set(StatusCode.Succeeded, new TLoginDto() { Token = tokenRes.Data });
            }
            else
            {
                result.Set(StatusCode.Failed);
                result.DeveloperMessages = [Identity_Messages.FailedGeneratingToken];
            }
        });
    }

    public virtual async Task<IResponse<TUserKey>> RequestRegisterOtp(TOTPRegisterRequestVM model)
    {
        var result = new Response<TUserKey>(ServiceName, OperationType.Add, _logger);
        return await result.FillAsync(async () =>
        {
            dynamic identifier = model.PhoneNumber.HasValue ? model.PhoneNumber
                : !string.IsNullOrWhiteSpace(model.Email) ? model.Email : null;

            if (identifier != null)
            {
                var uidRes = await userManagerService
                    .RequestRegisterUser(model, roleId);
                result.Set(uidRes.StatusCode, uidRes.Data);
            }
            else
            {
                result.Set(StatusCode.Canceled);
                result.DeveloperMessages = [Identity_Messages.InvalidEmailOrNumber];
            }
        });
    }

    public virtual async Task<IResponse<bool>> RegisterWithOtp(TOTPRegisterVM Model)
    {
        var result = new Response<bool>(ServiceName, OperationType.Add, _logger);

        return await result.FillAsync(async () =>
        {
            var isValidRes = await userManagerService.VerifyRegisterOtpCode(Model.Code, Model.UserId, Model.RoleId);
            if (isValidRes.IsSuccess)
            {
                result.Set(StatusCode.Succeeded);
            }
            else
            {
                result.Set(StatusCode.Failed);
            }
        });
    }
}
