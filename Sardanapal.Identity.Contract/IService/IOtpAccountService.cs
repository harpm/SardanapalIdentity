
using Sardanapal.Identity.Dto;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Contract.IService;

public interface IOtpAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM> : IAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto, new()
    where TRegisterVM : RegisterVM
{
    Task<IResponse<TUserKey>> RequestLoginOtp(OtpLoginRequestVM Model);
    Task<IResponse<TLoginDto>> LoginWithOtp(ValidateOtpVM<TUserKey> Model);
    Task<IResponse<TUserKey>> RequestRegisterOtp(OtpRegisterRequestVM model);
    Task<IResponse<bool>> RegisterWithOtp(ValidateOtpVM<TUserKey> Model);

}
