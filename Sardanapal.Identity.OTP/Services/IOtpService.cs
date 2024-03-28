using Sardanapal.Identity.ViewModel.Models;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpService<TKey, TNewVM, TValidateVM>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TNewVM : class
    where TValidateVM : class
{
    int expireTime { get; set; }

    Task<IResponse<Guid>> Add(TNewVM model);
    Task<IResponse<bool>> ValidateOtp(TValidateVM model);
    Task RemoveExpireds();
}
