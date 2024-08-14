using Sardanapal.Domain.Model;
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Domain.Model;

public abstract class RoleBase<TKey> : BaseEntityModel<TKey>, IRole<TKey>
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