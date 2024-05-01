using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.Domain.Model;

public interface IUserRoleBase<TUserKey, TRoleKey> : IBaseEntityModel<long>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
{
    TUserKey UserId { get; set; }
    TRoleKey RoleId { get; set; }
}

public class UserRoleBase<TUserKey, TRoleKey> : BaseEntityModel<long>, IUserRoleBase<TUserKey, TRoleKey>
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
    public virtual SardanapalRole<TRoleKey, TUserKey> Roles { get; set; }
}