using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpServiceBase<TUserKey, TKey, TNewVM, TValidateVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TNewVM : class, new()
    where TValidateVM : class, new()
{
    int expireTime { get; set; }
    Task<IResponse<bool>> ValidateOtp(TValidateVM model);
    Task RemoveExpireds();
}
