
using Sardanapal.Contract.IRepository;
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Contract.IRepository;

public interface IRoleRepository<TRoleKey, TRole>
    : IEFRepository<TRoleKey, TRole>
    where TRole : class, IRole<TRoleKey>, new()
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
{
}
