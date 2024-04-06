using Sardanapal.ViewModel.Models;

namespace Sardanapal.Identity.ViewModel.Models.VM;

public class OtpListItemVM<TKey> : BaseListItem<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{

}

public class OtpSearchVM
{

}

public class NewOtpVM<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public string Code { get; set; }
    public TUserKey UserId { get; set; }
    public DateTime ExpireTime { get; set; }
    public byte RoleId { get; set; }
}

public class OtpEditableVM<TKey> : NewOtpVM<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{

}

public class OtpVM
{

}

public class ValidateOtpVM<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public string Code { get; set; }
    public TUserKey UserId { get; set; }
    public byte RoleId { get; set; }
}

public class OtpRequestVM
{
    public string? Email { get; set; }
    public long? PhoneNumber { get; set; }
}