using Sardanapal.ViewModel.Models;

namespace Sardanapal.Identity.ViewModel.Models;

public class OtpListItemVM<TKey> : BaseListItem<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    
}

public abstract class OtpSearchVM
{

}

public abstract class NewOtpVM<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public string Code { get; set; }
    public TKey UserId { get; set; }
    public DateTime ExpireTime { get; set; }
    public byte RoleId { get; set; }
}

public abstract class OtpEditableVM<TKey> : NewOtpVM<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{

}

public abstract class OtpVM<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{

}