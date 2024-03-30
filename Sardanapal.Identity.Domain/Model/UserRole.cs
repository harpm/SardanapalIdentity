using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.Domain.Model;

public interface IUserRoleBase<TUserKey> : IBaseEntityModel<long>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    TUserKey UserId { get; set; }
    byte RoleId { get; set; }
}

public abstract class UserRoleBase<TUserKey, TUser, TRole> : BaseEntityModel<long>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : IUserBase<TUserKey>
    where TRole : IRoleBase<TUserKey>
{
    public TUserKey UserId { get; set; }
    
    public byte RoleId { get; set; }

    public virtual TRole Roles { get; set; }
}