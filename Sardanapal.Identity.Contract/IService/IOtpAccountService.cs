
using Sardanapal.Identity.Dto;
using Sardanapal.Identity.OTP.VM;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Contract.IService;

public interface IOtpAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM> : IAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto
    where TRegisterVM : RegisterVM
{
    Task<IResponse<TUserKey>> RequestLoginOtp(OtpLoginRequestVM Model);
    Task<IResponse<LoginDto>> LoginWithOtp(ValidateOtpVM<TUserKey> Model);
    Task<IResponse<TUserKey>> RequestRegisterOtp(OtpRegisterRequestVM model);
    Task<IResponse<bool>> RegisterWithOtp(ValidateOtpVM<TUserKey> Model);

}
