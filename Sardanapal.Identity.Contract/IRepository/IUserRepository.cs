
using Sardanapal.Contract.IRepository;
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Contract.IRepository;

public interface IUserRepository<TUserKey, TRoleKey, TUserModel, TUR>
    : ICrudRepository<TUserKey, TUserModel>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TUserModel : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, TRoleKey>, new()
{
    IEnumerable<TUR> FetchAllUserRoles();
    Task<IEnumerable<TUR>> FetchAllUserRolesAsync();

    long AddUserRole(TUR userRole);
    Task<long> AddUserRoleAsync(TUR userRole);
}
