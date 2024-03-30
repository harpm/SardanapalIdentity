using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.Domain.Model;

public interface IUserRoleBase<TUserKey> : IBaseEntityModel<long>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    TUserKey UserId { get; set; }
    byte RoleId { get; set; }
}

public class UserRoleBase<TUserKey> : BaseEntityModel<long>, IUserRoleBase<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public TUserKey UserId { get; set; }
    
    public byte RoleId { get; set; }

    public virtual RoleBase<byte, TUserKey> Roles { get; set; }
}