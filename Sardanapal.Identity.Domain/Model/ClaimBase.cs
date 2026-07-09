
using System.ComponentModel.DataAnnotations.Schema;
using Sardanapal.Domain.Model;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Share.Types;

namespace Sardanapal.Identity.Domain.Model;

public abstract class ClaimBase<TKey> : BaseEntityModel<TKey>, IClaim<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    [NotMapped]
    public abstract byte ClaimType { get; }
}

public abstract class ControllerActionClaimBase<TKey> : ClaimBase<TKey>, IControllerActionClaim<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public override byte ClaimType => (byte)SdClaimType.ControllerAction;

    public Guid ControllerId { get; set; }
    public byte ActionType { get; set; }
}

public class SardanapalClaim<TKey, TUserKey> : ControllerActionClaimBase<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual ICollection<UserClaimBase<TUserKey, TKey>> UserClaims { get; set; }
        = new HashSet<UserClaimBase<TUserKey, TKey>>();
}
