using Sardanapal.Domain.Model;
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Domain.Model;

public abstract class UserBase<TUserKey> : BaseEntityModel<TUserKey>
    , IUser<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual string FirstName { get; set; }
    public virtual string LastName { get; set; }
    public virtual string Username { get; set; }
    public virtual string HashedPassword { get; set; }
    public virtual string? Email { get; set; }
    public virtual bool VerifiedEmail { get; set; } = false;
    public virtual long? PhoneNumber { get; set; }
    public virtual bool VerifiedPhoneNumber { get; set; } = false;
}

public class SardanapalUser<TUserKey, TRoleKey> : UserBase<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
{
    public virtual ICollection<UserRoleBase<TUserKey, TRoleKey>> UserRoles { get; set; }
        = new HashSet<UserRoleBase<TUserKey, TRoleKey>>();
}