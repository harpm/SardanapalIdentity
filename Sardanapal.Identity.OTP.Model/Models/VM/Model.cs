using Sardanapal.ViewModel.Models;

namespace Sardanapal.Identity.OTP.Model.Models.VM;

public class OtpListItemVM<TKey> : BaseListItem<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public DateTime ExpireTime { get; set; }
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

public class OtpEditableVM<TUserKey> : NewOtpVM<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
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

public class OtpLoginRequestVM
{
    public string? Email { get; set; }
    public long? PhoneNumber { get; set; }
}

public class OtpRegisterRequestVM : OtpLoginRequestVM
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}