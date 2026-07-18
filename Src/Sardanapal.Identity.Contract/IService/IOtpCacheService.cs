
using Sardanapal.Contract.IService;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.ViewModel.Otp;

namespace Sardanapal.Identity.Contract.IService;

public interface IOtpCacheService<TUserKey, TKey, TOtpCacheModel, TNewVM, TEditableVM>
    : ICacheService<TOtpCacheModel, TKey, OtpSearchVM, CacheOtpVM<TUserKey, TKey>, TNewVM, TEditableVM>
    , IOtpServiceBase<TUserKey, TKey, TNewVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOtpCacheModel : IOTPModel<TUserKey, TKey>, new()
    where TNewVM : CacheNewOtpVM<TUserKey, TKey>, new()
    where TEditableVM : CacheOtpEditableVM<TUserKey, TKey>, new()
{

}
