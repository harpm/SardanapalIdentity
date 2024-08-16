
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.Contract.IService;
using Sardanapal.Contract.IService.ICrud;

namespace Sardanapal.Identity.Contract.IService;

public interface IOtpServiceBase<TUserKey, TKey, TNewVM, TValidateVM>
    : ICreateService<TKey, TNewVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TNewVM : class, new()
    where TValidateVM : ValidateOtpVM<TUserKey>, new()
{
    int expireTime { get; set; }
    Task<IResponse<bool>> ValidateOtp(TValidateVM model);
    Task RemoveExpireds();
}

public interface IOtpService<TUserKey, TKey, TSearchVM, TVM, TNewVM, TEditableVM, TValidateVM>
    : ICrudService<TKey, TSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpServiceBase<TUserKey, TKey, TNewVM, TValidateVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TSearchVM : OtpSearchVM, new()
    where TVM : OtpVM, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
    where TValidateVM : ValidateOtpVM<TUserKey>, new()
{

}