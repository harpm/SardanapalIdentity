using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.OTP.Models.Domain;
using Sardanapal.Identity.ViewModel.Models;
using Sardanapal.InterfacePanel.Service;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpService<TKey, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM> : _IServicePanel<TKey, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TListItemVM : OtpListItemVM<TKey>
    where TSearchVM : OtpSearchVM
    where TVM : OtpVM
    where TNewVM : NewOtpVM
    where TEditableVM : OtpEditableVM
{

}

public class OtpService<UnitOfWork, TKey, OtpEntity, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM> : _ServicePanel<UnitOfWork, TKey, OtpEntity, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM>
    where UnitOfWork : DbContext
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where OtpEntity : OTPCode<TKey>
    where TListItemVM : OtpListItemVM<TKey>
    where TSearchVM : OtpSearchVM
    where TVM : OtpVM
    where TNewVM : NewOtpVM
    where TEditableVM : OtpEditableVM
{
    public OtpService(UnitOfWork unitOfWork, IMapper _Mapper, IRequestService _Context)
        : base(unitOfWork, _Mapper, _Context)
    {

    }
}