
using Sardanapal.Domain.Model;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Share.Types;

namespace Sardanapal.Identity.Domain.Model;

public abstract class ClaimBase<TKey> : BaseEntityModel<TKey>, IClaim<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public string Boundery { get; set; }
    public ClaimActionTypes ActionType { get; set; }
}

public class SardanapalClaim<TKey, TUserKey> : ClaimBase<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual ICollection<UserClaimBase<TUserKey, TKey>> UserRoles { get; set; }
        = new HashSet<UserClaimBase<TUserKey, TKey>>();
}