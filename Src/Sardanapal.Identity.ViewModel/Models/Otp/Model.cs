using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Models;

namespace Sardanapal.Identity.ViewModel.Otp;

public record OtpListItemVM<TKey> : BaseListItem<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public virtual DateTime ExpireTime { get; set; }
}

public record OtpSearchVM;

public record NewOtpVM<TUserKey>: NewUserVM
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual string Code { get; set; }
    public virtual TUserKey UserId { get; set; }
    public virtual DateTime ExpireTime { get; set; }
    public virtual byte RoleId { get; set; }
    public virtual string Recipient { get; set; }
}

public record OtpEditableVM<TUserKey> : UserEditableVM
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual string Code { get; set; }
    public virtual TUserKey UserId { get; set; }
    public virtual DateTime ExpireTime { get; set; }
    public virtual byte RoleId { get; set; }
    public virtual string Recipient { get; set; }
}

public record OtpVM<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual string Code { get; set; }
    public virtual TUserKey UserId { get; set; }
    public virtual DateTime ExpireTime { get; set; }
    public virtual byte RoleId { get; set; }
    public virtual string Recipient { get; set; }
}

public record OTPResponseVM<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual TUserKey UserId { get; set; }
    public virtual byte RoleId { get; set; }
    public virtual string Code { get; set; }
}

public record OtpRequestVM
{
    public virtual string? Email { get; set; }
    public virtual ulong? PhoneNumber { get; set; }
    public virtual byte Role { get; set; }
}
