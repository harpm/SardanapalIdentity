
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.Contract.IService;
using Sardanapal.Contract.IService.ICrud;

namespace Sardanapal.Identity.Contract.IService;

public interface IOtpServiceBase<TUserKey, TKey, TNewVM, TOTPLoginVM, TOTPRegisterVM>
    : ICreateService<TKey, TNewVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TNewVM : class, new()
    where TOTPLoginVM : OTPLoginVM<TUserKey>, new()
    where TOTPRegisterVM : OTPRegisterVM<TUserKey>, new()
{
    Task<IResponse<bool>> ValidateOtpRegister(TOTPRegisterVM model);
    Task<IResponse<bool>> ValidateOtpLogin(TOTPLoginVM model);
    Task RemoveExpireds();
}

public interface IOtpService<TUserKey, TKey, TSearchVM, TVM, TNewVM, TEditableVM, TOTPLoginVM, TOTPRegisterVM>
    : ICrudService<TKey, TSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpServiceBase<TUserKey, TKey, TNewVM, TOTPLoginVM, TOTPRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TSearchVM : OtpSearchVM, new()
    where TVM : OtpVM, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
    where TOTPLoginVM : OTPLoginVM<TUserKey>, new()
    where TOTPRegisterVM : OTPRegisterVM<TUserKey>, new()
{

}