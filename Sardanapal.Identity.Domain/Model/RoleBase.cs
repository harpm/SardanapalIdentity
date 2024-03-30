using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.Domain.Model;

public interface IRoleBase<TKey> : IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    string Title { get; set; }
}

public class RoleBase<TKey, TUser, TUR> : BaseEntityModel<TKey>, IRoleBase<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TUser : class, IUserBase<TKey>
    where TUR : UserRoleBase<TKey, TUser, RoleBase<TKey, TUser, TUR>>
{
    public virtual string Title { get; set; }
    public virtual ICollection<TUR> UserRoles { get; set; }
        = new HashSet<TUR>();
}