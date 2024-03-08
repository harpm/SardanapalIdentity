using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.Domain.Model;

public class UserRole<TKey> : BaseEntityModel<long>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TKey UserId { get; set; }
    public byte RoleId { get; set; }
}