using Sardanapal.Domain.Model;
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Domain.Model;

public abstract class UserBase<TUserKey> : LogicalEntityModel<TUserKey, TUserKey>
    , IUser<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual string FirstName { get; set; }
    public virtual string LastName { get; set; }
    public virtual string Username { get; set; }
    public virtual string HashedPassword { get; set; }
    public virtual string? Email { get; set; }
    public virtual bool VerifiedEmail { get; set; } = false;
    public virtual ulong? PhoneNumber { get; set; }
    public virtual bool VerifiedPhoneNumber { get; set; } = false;
}

public class SardanapalUser<TUserKey, TRoleKey, TClaimKey> : UserBase<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TClaimKey : IComparable<TClaimKey>, IEquatable<TClaimKey>
{
    public virtual ICollection<UserRoleBase<TUserKey, TRoleKey>> UserRoles { get; set; }
        = new HashSet<UserRoleBase<TUserKey, TRoleKey>>();


    public virtual ICollection<UserClaimBase<TUserKey, TClaimKey>> UserClaims { get; set; }
        = new HashSet<UserClaimBase<TUserKey, TClaimKey>>();
}
