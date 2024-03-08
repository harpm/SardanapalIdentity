using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.Domain.Model;

public interface IRoleBase<TKey> : IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{

}

public class RoleBase<TKey> : BaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public virtual string Title { get; set; }
}