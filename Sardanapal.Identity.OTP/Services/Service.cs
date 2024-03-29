using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.OTP.Models.Domain;
using Sardanapal.Identity.ViewModel.Models;
using Sardanapal.InterfacePanel.Service;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpService<TUserKey, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM> : _IServicePanel<Guid, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpService<TUserKey, TNewVM, ValidateOtpVM<TUserKey>>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TListItemVM : OtpListItemVM<Guid>
    where TSearchVM : OtpSearchVM
    where TVM : OtpVM<TUserKey>
    where TNewVM : NewOtpVM<TUserKey>
    where TEditableVM : OtpEditableVM<TUserKey>
{

}

public class OtpService<UnitOfWork, TUserKey, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM> : _ServicePanel<UnitOfWork, Guid, OTPCode<TUserKey>, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpService<TUserKey, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM>, IOtpService<TUserKey, TNewVM, ValidateOtpVM<TUserKey>>
    where UnitOfWork : DbContext
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TListItemVM : OtpListItemVM<Guid>
    where TSearchVM : OtpSearchVM
    where TVM : OtpVM<TUserKey>
    where TNewVM : NewOtpVM<TUserKey>
    where TEditableVM : OtpEditableVM<TUserKey>
{
    public int expireTime { get; set; }

    public override string ServiceName => "OtpService";

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

    public Task<IResponse<bool>> ValidateOtp(ValidateOtpVM<TUserKey> model)
    {
        throw new NotImplementedException();
    }
}