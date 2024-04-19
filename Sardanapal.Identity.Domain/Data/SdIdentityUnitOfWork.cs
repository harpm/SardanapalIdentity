using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.DomainModel.UnitOfWork;

namespace Sardanapal.Identity.Domain.Data;

public interface ISdIdentityUnitOfWorkBase<TKey, TUser, TRole, TUR> : ISardanapalUnitOfWork
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TUser : class, IUserBase<TKey>, new()
    where TRole : class, IRoleBase<byte>, new()
    where TUR : class, IUserRoleBase<TKey>, new()
{
    DbSet<TUser> Users { get; set; }
    DbSet<TRole> Roles { get; set; }
    DbSet<TUR> UserRoles { get; set; }
}

public abstract class SdIdentityUnitOfWorkBase<TUserKey, TUser, TRole, TUR> : SardanapalUnitOfWork
    , ISdIdentityUnitOfWorkBase<TUserKey, TUser, TRole, TUR>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUserBase<TUserKey>, new()
    where TRole : class, IRoleBase<byte>, new()
    where TUR : class, IUserRoleBase<TUserKey>, new()
{
    public DbSet<TUser> Users { get; set; }
    public DbSet<TRole> Roles { get; set; }
    public DbSet<TUR> UserRoles { get; set; }
    
}