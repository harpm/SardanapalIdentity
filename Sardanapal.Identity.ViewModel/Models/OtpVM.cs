using Sardanapal.ViewModel.Models;

namespace Sardanapal.Identity.ViewModel.Models;

public class OtpListItemVM<TKey> : BaseListItem<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
}

public class OtpSearchVM
{

}

public class NewOtpVM
{

}

public class OtpEditableVM : NewOtpVM
{

}

public class OtpVM
{

}