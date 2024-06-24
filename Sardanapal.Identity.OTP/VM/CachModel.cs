using Sardanapal.RedisCach.Models;
using Sardanapal.ViewModel.Models;

namespace Sardanapal.Identity.OTP.VM;

public class CachOtpListItemVM<TUserKey, TKey> : BaseListItem<TKey>, ICachModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TUserKey UserId { get; set; }
    public DateTime ExpireTime { get; set; }
}

public class CachNewOtpVM<TUserKey, TKey> : NewOtpVM<TUserKey>, ICachModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TKey Id { get; set; }
}

public class CachOtpEditableVM<TUserKey, TKey> : OtpEditableVM<TUserKey>, ICachModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TKey Id { get; set; }
}

public class CachOtpVM<TUserKey, TKey> : ICachModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TKey Id { get; set; }
    public TUserKey UserId { get; set; }
}