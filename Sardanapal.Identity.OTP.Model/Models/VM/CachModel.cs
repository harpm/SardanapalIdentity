using Sardanapal.RedisCach.Models;
using Sardanapal.ViewModel.Models;

namespace Sardanapal.Identity.OTP.Model.Models.VM;

public class CachOtpListItemVM<TUserKey> : BaseListItem<Guid>, ICachModel<Guid>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public TUserKey UserId { get; set; }
    public DateTime ExpireTime { get; set; }
}

public class CachNewOtpVM<TUserKey> : ICachModel<Guid>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public TUserKey UserId { get; set; }
    public DateTime ExpireTime { get; set; }
    public byte RoleId { get; set; }
}

public class CachOtpEditableVM<TUserKey> : NewOtpVM<TUserKey>, ICachModel<Guid>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public Guid Id { get; set; }
}

public class CachOtpVM<TUserKey> : ICachModel<Guid>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public Guid Id { get; set; }
    public TUserKey UserId { get; set; }
}