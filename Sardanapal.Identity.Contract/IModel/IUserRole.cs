using Sardanapal.Domain.Model;

namespace Sardanapal.Identity.Contract.IModel;

public interface IUserRole<TUserKey, TRoleKey> : IBaseEntityModel<long>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
{
    TUserKey UserId { get; set; }
    TRoleKey RoleId { get; set; }
}