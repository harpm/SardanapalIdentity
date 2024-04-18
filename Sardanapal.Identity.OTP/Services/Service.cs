using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sardanapal.Ef.Service.Services;
using Sardanapal.Identity.OTP.Model.Models.VM;
using Sardanapal.Identity.OTP.Models.Domain;
using Sardanapal.InterfacePanel.Service;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpService<TUserKey, TKey, TSearchVM, TVM, TNewVM, TEditableVM>
    : ICrudService<TUserKey, TSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpServiceBase<TUserKey, TUserKey, TNewVM, ValidateOtpVM<TUserKey>>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TSearchVM : OtpSearchVM, new()
    where TVM : OtpVM, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
{

}

public class OtpService<TContext, TUserKey, TKey, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM>
    : EfCrudService<TContext, TKey, OTPModel<TUserKey, TKey>, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpServiceBase<TUserKey, TKey, TNewVM, ValidateOtpVM<TUserKey>>
    where TContext : DbContext
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TListItemVM : OtpListItemVM<TKey>
    where TSearchVM : OtpSearchVM, new()
    where TVM : OtpVM, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
{
    public int expireTime { get; set; }

    public override string ServiceName => "OtpService";

    public OtpService(TContext unitOfWork, IMapper _Mapper, IRequestService _Context, int expireTime)
        : base(unitOfWork, _Mapper, _Context)
    {
        this.expireTime = expireTime;
    }

    public async Task RemoveExpireds()
    {
        this.UnitOfWork.RemoveRange(GetCurrentService().Where(x => x.ExpireTime <= DateTime.UtcNow));
        await this.UnitOfWork.SaveChangesAsync();
    }

    public Task<IResponse<bool>> ValidateOtp(ValidateOtpVM<TUserKey> model)
    {
        throw new NotImplementedException();
    }
}