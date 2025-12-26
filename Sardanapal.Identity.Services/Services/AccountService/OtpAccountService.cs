
using Microsoft.Extensions.Logging;
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Otp;

namespace Sardanapal.Identity.Services.Services.AccountService;

public abstract class OtpAccountServiceBase<TOtpUserManager, TUserKey, TUser, TUR, TLoginVM, TLoginDto, TRegisterVM, TOTPLoginRequestVM, TOTPLoginVM, TOTPRegisterRequestVM, TOTPRegisterVM, TUserEditable>
    : AccountServiceBase<TOtpUserManager, TUserKey, TUser, TLoginVM, TLoginDto, TRegisterVM, TUserEditable>
    , IOtpAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM, TOTPLoginRequestVM, TOTPLoginVM, TOTPRegisterRequestVM, TOTPRegisterVM, TUserEditable>
    where TOtpUserManager : class, IOtpUserManager<TUserKey, TUser, TRegisterVM, TOTPRegisterRequestVM, TUserEditable>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TLoginVM : LoginVM, new()
    where TLoginDto : LoginDto, new()
    where TRegisterVM : RegisterVM<byte>, new()
    where TOTPLoginRequestVM : OtpLoginRequestVM<byte>, new()
    where TOTPRegisterRequestVM : OtpRegisterRequestVM, new()
    where TOTPLoginVM : OTPLoginVM<TUserKey>, new()
    where TOTPRegisterVM : OTPRegisterVM<TUserKey>, new()
    where TUserEditable : UserEditableVM, new()
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
                var uid = await _userManagerService
                    .RequestLoginUser(identifier, model.Role);
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
            var tokenRes = await _userManagerService.VerifyLoginOtpCode(Model.Code, Model.UserId, Model.RoleId);

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
                var uidRes = await _userManagerService
                    .RequestRegisterUser(model, model.Role);
                result.Set(uidRes.StatusCode, uidRes.Data);
            }
            else
            {
                result.Set(StatusCode.Canceled);
                result.DeveloperMessages = [Identity_Messages.InvalidEmailOrNumber];
            }
        });
    }

    public virtual async Task<IResponse> RegisterWithOtp(TOTPRegisterVM Model)
    {
        return await _userManagerService.VerifyRegisterOtpCode(Model.Code, Model.UserId, Model.RoleId);
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
                var uid = await _userManagerService
                    .RequestResetPassword(identifier);
                result.Set(StatusCode.Succeeded, uid);
            }
            else
            {
                result.Set(StatusCode.Canceled, Identity_Messages.InvalidEmailOrNumber);
            }
        });

        return result;
    }

    public virtual async Task<IResponse> ResetPassword(ResetPasswordVM<TUserKey> model) =>
        await _userManagerService.ResetPassword(model.Code, model.UserId, model.RoleId, model.NewPassword);
}
