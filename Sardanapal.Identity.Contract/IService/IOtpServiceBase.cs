
using Sardanapal.Interface.IService.ICrud;
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.OTP.VM;

namespace Sardanapal.Identity.Contract.IServices;

public interface IOtpServiceBase<TUserKey, TKey, TNewVM, TValidateVM> : ICreateService<TKey, TNewVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TNewVM : class, new()
    where TValidateVM : ValidateOtpVM<TUserKey>, new()
{
    int expireTime { get; set; }
    Task<IResponse<bool>> ValidateOtp(TValidateVM model);
    Task RemoveExpireds();
}
