using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.Domain.Model;

public interface IRoleBase<TKey> : IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    string Title { get; set; }
}

public class RoleBase<TKey, TUserKey> : BaseEntityModel<TKey>, IRoleBase<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual string Title { get; set; }
    public virtual ICollection<UserRoleBase<TUserKey, TKey>> UserRoles { get; set; }
        = new HashSet<UserRoleBase<TUserKey, TKey>>();
}