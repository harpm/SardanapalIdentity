using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.Domain.Model;

public interface IUserBase<TUserKey> : IBaseEntityModel<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    string FirstName { get; set; }
    string LastName { get; set; }
    string Username { get; set; }
    string HashedPassword { get; set; }
    string? Email { get; set; }
    bool VerifiedEmail { get; set; }
    long? PhoneNumber { get; set; }
    bool VerifiedPhoneNumber { get; set; }
}

public abstract class UserBase<TUserKey> : BaseEntityModel<TUserKey>
    , IUserBase<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual string FirstName { get; set; }
    public virtual string LastName { get; set; }
    public virtual string Username { get; set; }
    public virtual string HashedPassword { get; set; }
    public virtual string? Email { get; set; }
    public virtual bool VerifiedEmail { get; set; }
    public virtual long? PhoneNumber { get; set; }
    public virtual bool VerifiedPhoneNumber { get; set; }
}

public class SardanapalUser<TUserKey, TRoleKey> : UserBase<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
{
    public virtual ICollection<UserRoleBase<TUserKey, TRoleKey>> UserRoles { get; set; }
        = new HashSet<UserRoleBase<TUserKey, TRoleKey>>();
}