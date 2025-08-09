
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Otp;

namespace Sardanapal.Identity.Contract.IService;

public interface IOtpAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM, TOTPLoginRequestVM, TOTPLoginVM, TOTPRegisterRequestVM, TOTPRegisterVM>
    : IAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto, new()
    where TRegisterVM : RegisterVM, new()
    where TOTPLoginRequestVM : OtpLoginRequestVM, new()
    where TOTPRegisterRequestVM : OtpRegisterRequestVM, new()
    where TOTPLoginVM : OTPLoginVM<TUserKey>, new()
    where TOTPRegisterVM : OTPRegisterVM<TUserKey>, new()
{
    Task<IResponse<TUserKey>> RequestLoginOtp(TOTPLoginRequestVM Model);
    Task<IResponse<TLoginDto>> LoginWithOtp(TOTPLoginVM Model);
    Task<IResponse<TUserKey>> RequestRegisterOtp(TOTPRegisterRequestVM model);
    Task<IResponse<bool>> RegisterWithOtp(TOTPRegisterVM Model);

}
