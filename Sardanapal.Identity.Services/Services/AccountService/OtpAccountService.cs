
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Dto;
using Sardanapal.Identity.Share.Resources;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.AccountService;

public abstract class OtpAccountServiceBase<TOtpUserManager, TUserKey, TUser, TUR, TLoginVM, TLoginDto, TRegisterVM>
    : AccountServiceBase<TOtpUserManager, TUserKey, TUser, TLoginVM, TLoginDto, TRegisterVM>
    , IOtpAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TOtpUserManager : class, IOtpUserManager<TUserKey, TUser>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto, new()
    where TRegisterVM : RegisterVM
{
    protected override string ServiceName => "OTP AccountService";
    public OtpAccountServiceBase(TOtpUserManager _userManagerService)
        : base(_userManagerService)
    {

    }

    public virtual async Task<IResponse<TUserKey>> RequestLoginOtp(OtpLoginRequestVM model)
    {
        var result = new Response<TUserKey>(ServiceName, OperationType.Function);

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
                result.DeveloperMessages = [Service_Messages.InvalidEmailOrNumber];
            }
        });
    }

    public virtual async Task<IResponse<TLoginDto>> LoginWithOtp(ValidateOtpVM<TUserKey> Model)
    {
        var result = new Response<TLoginDto>();

        return await result.FillAsync(async () =>
        {
            var token = await userManagerService.VerifyLoginOtpCode(Model.Code, Model.UserId, Model.RoleId);

            if (!string.IsNullOrWhiteSpace(token))
            {
                result.Set(StatusCode.Succeeded, new TLoginDto() { Token = token });
            }
            else
            {
                result.Set(StatusCode.Failed);
                result.DeveloperMessages = [Service_Messages.FailedGeneratingToken];
            }
        });
    }

    public virtual async Task<IResponse<TUserKey>> RequestRegisterOtp(OtpRegisterRequestVM model)
    {
        var result = new Response<TUserKey>();
        return await result.FillAsync(async () =>
        {
            dynamic identifier = model.PhoneNumber.HasValue ? model.PhoneNumber
                : !string.IsNullOrWhiteSpace(model.Email) ? model.Email : null;

            if (identifier != null)
            {
                var uid = await userManagerService
                    .RequestRegisterUser(identifier, model.FirstName, model.LastName, roleId);
                result.Set(StatusCode.Succeeded, uid);
            }
            else
            {
                result.Set(StatusCode.Canceled);
                result.DeveloperMessages = [Service_Messages.InvalidEmailOrNumber];
            }
        });
    }

    public virtual async Task<IResponse<bool>> RegisterWithOtp(ValidateOtpVM<TUserKey> Model)
    {
        var result = new Response<bool>();

        return await result.FillAsync(async () =>
        {
            var isValid = await userManagerService.VerifyRegisterOtpCode(Model.Code, Model.UserId, Model.RoleId);
            if (isValid)
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
