using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.OTP.Models.Domain;
using Sardanapal.Identity.ViewModel.Models;
using Sardanapal.InterfacePanel.Service;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpService<TKey, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM> : _IServicePanel<Guid, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpService<TKey, TNewVM, ValidateOtpVM<TKey>>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TListItemVM : OtpListItemVM<Guid>
    where TSearchVM : OtpSearchVM
    where TVM : OtpVM<TKey>
    where TNewVM : NewOtpVM<TKey>
    where TEditableVM : OtpEditableVM<TKey>
{

}

public class OtpService<UnitOfWork, TKey, OtpEntity, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM> : _ServicePanel<UnitOfWork, Guid, OtpEntity, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpService<TKey, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM>
    where UnitOfWork : DbContext
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where OtpEntity : OTPCode<TKey>
    where TListItemVM : OtpListItemVM<Guid>
    where TSearchVM : OtpSearchVM
    where TVM : OtpVM<TKey>
    where TNewVM : NewOtpVM<TKey>
    where TEditableVM : OtpEditableVM<TKey>
{
    public int expireTime { get; set; }
    public OtpService(UnitOfWork unitOfWork, IMapper _Mapper, IRequestService _Context, int expireTime)
        : base(unitOfWork, _Mapper, _Context)
    {
        this.expireTime = expireTime;
    }

    public async Task RemoveExpireds()
    {
        this.UnitOfWork.RemoveRange(GetCurrentService().Where(x => x.ExpireTime <= DateTime.UtcNow));
        await this.UnitOfWork.SaveChangesAsync();
    }

    public Task<IResponse<bool>> ValidateOtp(ValidateOtpVM<TKey> model)
    {
        throw new NotImplementedException();
    }
}