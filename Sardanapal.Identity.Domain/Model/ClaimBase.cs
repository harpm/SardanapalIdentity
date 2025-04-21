
using Sardanapal.Domain.Model;
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Domain.Model;

public abstract class ClaimBase<TKey> : BaseEntityModel<TKey>, IClaim<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public Guid ControllerId { get; set; }
    public byte ActionType { get; set; }
}

public class SardanapalClaim<TKey, TUserKey> : ClaimBase<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual ICollection<UserClaimBase<TUserKey, TKey>> UserRoles { get; set; }
        = new HashSet<UserClaimBase<TUserKey, TKey>>();
}