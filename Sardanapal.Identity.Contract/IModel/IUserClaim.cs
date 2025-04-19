using Sardanapal.Contract.IModel;

namespace Sardanapal.Identity.Contract.IModel;

public interface IUserClaim<TUserKey, TClaimKey> : IBaseEntityModel<long>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TClaimKey : IComparable<TClaimKey>, IEquatable<TClaimKey>
{
    TUserKey UserId { get; set; }
    TClaimKey ClaimId { get; set; }
}