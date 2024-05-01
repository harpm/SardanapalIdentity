using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.Domain.Model;

public interface IRoleBase<TKey> : IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    string Title { get; set; }
}

public abstract class RoleBase<TKey> : BaseEntityModel<TKey>, IRoleBase<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public virtual string Title { get; set; }
}

public class SardanapalRole<TKey, TUserKey> : RoleBase<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public virtual ICollection<UserRoleBase<TUserKey, TKey>> UserRoles { get; set; }
        = new HashSet<UserRoleBase<TUserKey, TKey>>();
}