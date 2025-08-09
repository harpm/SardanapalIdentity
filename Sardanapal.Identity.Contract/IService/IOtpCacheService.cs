
using Sardanapal.Contract.IService;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.ViewModel.Otp;

namespace Sardanapal.Identity.Contract.IService;

public interface IOtpCachService<TUserKey, TKey, TOtpCachModel, TNewVM, TEditableVM, TOTPLoginVM, TOTPRegisterVM>
    : ICacheService<TOtpCachModel, TKey, OtpSearchVM, CachOtpVM<TUserKey, TKey>, TNewVM, TEditableVM>
    , IOtpServiceBase<TUserKey, TKey, TNewVM, TOTPLoginVM, TOTPRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOtpCachModel : IOTPModel<TUserKey, TKey>, new()
    where TNewVM : CachNewOtpVM<TUserKey, TKey>, new()
    where TEditableVM : CachOtpEditableVM<TUserKey, TKey>, new()
    where TOTPLoginVM : OTPLoginVM<TUserKey>, new()
    where TOTPRegisterVM : OTPRegisterVM<TUserKey>, new()
{

}