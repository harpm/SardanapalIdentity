
using Sardanapal.Contract.IService;
using Sardanapal.Contract.IService.ICrud;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Contract.IService;

public interface IOtpServiceBase<TUserKey, TKey, TNewVM>
    : ICreateService<TKey, TNewVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TNewVM : class, new()
{
    [Obsolete("Use expire time on the cache provider instead of this method")]
    Task RemoveExpireds();
}

public interface IOtpService<TUserKey, TKey, TVM, TNewVM, TEditableVM>
    : ICrudService<TKey, OtpSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpServiceBase<TUserKey, TKey, TNewVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TVM : OtpVM<TUserKey>, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
{
    Task<IResponse<TVM>> ValidateCode(TNewVM model);

}
