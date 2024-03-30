using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.ModelBase.UnitOfWork;

namespace Sardanapal.Identity.Domain.Data;

public abstract class SdIdentityUnitOfWorkBase<TKey, TUser, TRole, TUR> : DbContext, IUnitOfWork
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TUser : class, IUserBase<TKey>, new()
    where TRole : class, IRoleBase<byte>, new()
    where TUR : class, IUserRoleBase<TKey>, new()
{
    public DbSet<TUser> Users { get; set; }
    public DbSet<TRole> Roles { get; set; }
    public DbSet<TUR> UserRoles { get; set; }
}

public class SdIdentityUnitOfWork<TKey, TUser, TRole, TUR> : SdIdentityUnitOfWorkBase<TKey, TUser, TRole, TUR>
    , IUnitOfWork
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TUser : class, IUserBase<TKey>, new()
    where TRole : class, IRoleBase<byte>, new()
    where TUR : class, IUserRoleBase<TKey>, new()
{

}