using Sardanapal.ViewModel.Models;

namespace Sardanapal.Identity.ViewModel.Models;

public class OtpListItemVM<TKey> : BaseListItem<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    
}

public class OtpSearchVM
{

}

public class NewOtpVM<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public string Code { get; set; }
    public TKey UserId { get; set; }
    public DateTime ExpireTime { get; set; }
    public byte RoleId { get; set; }
}

public class OtpEditableVM<TKey> : NewOtpVM<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{

}

public class OtpVM<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{

}

public class ValidateOtpVM<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public string Code { get; set; }
    public TKey UserId { get; set; }
    public byte RoleId { get; set; }
}