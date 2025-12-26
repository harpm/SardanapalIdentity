
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Otp;

namespace Sardanapal.Identity.Contract.IService;

public interface IOtpAccountService<TUserKey, TRegisterVM, TUserEditable>
    : IAccountService<TUserKey, TRegisterVM, TUserEditable>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRegisterVM : RegisterVM<byte>, new()
    where TUserEditable : UserEditableVM, new()
{
    Task<IResponse<TUserKey>> RequestLoginOtp(OtpRequestVM model);
    Task<IResponse<LoginDto>> LoginWithOtp(OTPResponseVM<TUserKey> model);
    Task<IResponse<TUserKey>> RequestRegisterOtp(OtpRequestVM model);
    Task<IResponse> RegisterWithOtp(OTPResponseVM<TUserKey> model);
    Task<IResponse<TUserKey>> RequestResetPassword(ResetPasswordRequestVM model);
    Task<IResponse> ResetPassword(ResetPasswordVM<TUserKey> model);
}
