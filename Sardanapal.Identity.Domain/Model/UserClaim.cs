
using Sardanapal.Domain.Model;
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Domain.Model;

public abstract class UserClaimBase<TUserKey, TClaimKey> : BaseEntityModel<long>, IUserClaim<TUserKey, TClaimKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TClaimKey : IComparable<TClaimKey>, IEquatable<TClaimKey>
{
    public TUserKey UserId { get; set; }

    public TClaimKey ClaimId { get; set; }

}

public class SardanapalUserClaim<TUserKey, TRoleKey, TClaimKey> : UserClaimBase<TUserKey, TRoleKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TClaimKey : IComparable<TClaimKey>, IEquatable<TClaimKey>
{
    public virtual SardanapalUser<TUserKey, TRoleKey, TClaimKey> Users { get; set; }
    public virtual SardanapalClaim<TClaimKey, TUserKey> Claims { get; set; }
}