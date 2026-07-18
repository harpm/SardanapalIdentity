using Sardanapal.Domain.Model;
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Domain.Model;

public abstract class UserRoleBase<TUserKey, TRoleKey> : BaseEntityModel<long>, IUserRole<TUserKey, TRoleKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
{
    public TUserKey UserId { get; set; }
    
    public TRoleKey RoleId { get; set; }

}

public class SardanapalUserRole<TUserKey, TRoleKey, TClaimKey> : UserRoleBase<TUserKey, TRoleKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TClaimKey : IComparable<TClaimKey>, IEquatable<TClaimKey>
{
    public virtual SardanapalUser<TUserKey, TRoleKey, TClaimKey> Users { get; set; }
    public virtual SardanapalRole<TRoleKey, TUserKey> Roles { get; set; }
}