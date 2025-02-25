using Sardanapal.Contract.IModel;

namespace Sardanapal.Identity.ViewModel.Otp;

public record CachOtpListItemVM<TUserKey, TKey>(TUserKey UserId, DateTime ExpireTime)
    : OtpListItemVM<TKey>
    , ICachModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>;

public record CachNewOtpVM<TUserKey, TKey> : NewOtpVM<TUserKey>, ICachModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TKey Id { get; set; }
}

public record CachOtpEditableVM<TUserKey, TKey> : OtpEditableVM<TUserKey>, ICachModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TKey Id { get; set; }
}

public record CachOtpVM<TUserKey, TKey> : OtpVM, ICachModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TKey Id { get; set; }
    public TUserKey UserId { get; set; }
}