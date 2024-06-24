using Sardanapal.ViewModel.Models;

namespace Sardanapal.Identity.OTP.VM;

public class OtpListItemVM<TKey> : BaseListItem<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public virtual DateTime ExpireTime { get; set; }
}

public class OtpSearchVM
{

}

public class NewOtpVM<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual string Code { get; set; }
    public virtual TUserKey UserId { get; set; }
    public virtual DateTime ExpireTime { get; set; }
    public virtual byte RoleId { get; set; }
}

public class OtpEditableVM<TUserKey> : NewOtpVM<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{

}

public class OtpVM
{

}

public class ValidateOtpVM<TUserKey> : OtpEditableVM<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{

}

public class OtpLoginRequestVM
{
    public virtual string? Email { get; set; }
    public virtual long? PhoneNumber { get; set; }
}

public class OtpRegisterRequestVM : OtpLoginRequestVM
{
    public virtual string FirstName { get; set; }
    public virtual string LastName { get; set; }
}