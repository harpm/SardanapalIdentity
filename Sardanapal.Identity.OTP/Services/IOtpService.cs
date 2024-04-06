using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpService<TUserKey, TNewVM, TValidateVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TNewVM : class, new()
    where TValidateVM : class, new()
{
    int expireTime { get; set; }

    Task<IResponse<Guid>> Add(TNewVM model);
    Task<IResponse<bool>> ValidateOtp(TValidateVM model);
    Task RemoveExpireds();
}
