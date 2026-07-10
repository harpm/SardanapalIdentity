using Sardanapal.Contract.IModel;

namespace Sardanapal.Identity.ViewModel.Otp;

public record CacheOtpListItemVM<TUserKey, TKey>
    : OtpListItemVM<TKey>
    , ICacheModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TUserKey UserId { get; init; }
}

public record CacheNewOtpVM<TUserKey, TKey> : NewOtpVM<TUserKey>, ICacheModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TKey Id { get; set; }
}

public record CacheOtpEditableVM<TUserKey, TKey> : OtpEditableVM<TUserKey>, ICacheModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TKey Id { get; set; }
}

public record CacheOtpVM<TUserKey, TKey> : OtpVM<TUserKey>, ICacheModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TKey Id { get; set; }
    public TUserKey UserId { get; set; }
}
