using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.Domain.Model;

public interface IRoleBase<TKey> : IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    string Title { get; set; }
}

public abstract class RoleBase<TRole, TKey, TUser, TUR> : BaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TRole : IRoleBase<TKey>
    where TUser : class, IUserBase<TKey>
    where TUR : UserRoleBase<TKey, TUser, TRole>
{
    public virtual string Title { get; set; }
    public virtual ICollection<TUR> UserRoles { get; set; }
        = new HashSet<TUR>();
}