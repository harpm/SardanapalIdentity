using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.Domain.Model;

public abstract class UserRoleBase<TKey> : BaseEntityModel<long>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TKey UserId { get; set; }
    public byte RoleId { get; set; }
}