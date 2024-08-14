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

public class SardanapalUserRole<TUserKey, TRoleKey> : UserRoleBase<TUserKey, TRoleKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
{
    public virtual SardanapalUser<TUserKey, TRoleKey> Users { get; set; }
    public virtual SardanapalRole<TRoleKey, TUserKey> Roles { get; set; }
}