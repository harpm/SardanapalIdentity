
using Sardanapal.Contract.IRepository;
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Contract.IRepository;

public interface IEFRoleRepository<TRoleKey, TRole>
    : IEFCrudRepository<TRoleKey, TRole>
    where TRole : class, IRoleBase<TRoleKey>, new()
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
{

}

public interface IRoleRepository<TRoleKey, TRole>
    : ICrudRepository<TRoleKey, TRole>
    where TRole : class, IRoleBase<TRoleKey>, new()
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
{

}
