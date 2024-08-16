using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sardanapal.Ef.Service.Services;
using Sardanapal.Identity.OTP.VM;
using Sardanapal.Identity.OTP.Domain;
using Sardanapal.InterfacePanel.Service;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.OTP.Services;

public class OtpService<TContext, TUserKey, TKey, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM, TValidateVM>
    : EfCrudService<TContext, TKey, OTPModel<TUserKey, TKey>, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpService<TUserKey, TKey, TSearchVM, TVM, TNewVM, TEditableVM, TValidateVM>
    where TContext : DbContext
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TListItemVM : OtpListItemVM<TKey>
    where TSearchVM : OtpSearchVM, new()
    where TVM : OtpVM, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
    where TValidateVM : ValidateOtpVM<TUserKey>, new()
{
    public int expireTime { get; set; }

    public override string ServiceName => "OtpService";

    public OtpService(TContext context, IMapper _Mapper, IRequestService _request, int expireTime)
        : base(context, _Mapper, _request)
    {
        this.expireTime = expireTime;
    }

    public async Task RemoveExpireds()
    {
        this.UnitOfWork.RemoveRange(GetCurrentService().Where(x => x.ExpireTime <= DateTime.UtcNow));
        await this.UnitOfWork.SaveChangesAsync();
    }

    public Task<IResponse<bool>> ValidateOtp(TValidateVM model)
    {
        throw new NotImplementedException();
    }
}