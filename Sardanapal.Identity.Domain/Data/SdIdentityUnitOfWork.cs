using Microsoft.EntityFrameworkCore;
using Sardanapal.Domain.UnitOfWork;
using Sardanapal.Identity.Authorization.Data;
using System.Security.Claims;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Contract.Data;
using Sardanapal.Contract.IModel;

namespace Sardanapal.Identity.Domain.Data;

public interface ISdIdentityUnitOfWork<TUserKey, TRoleKey, TUser, TRole, TUR> : ISdUnitOfWork
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRole<TRoleKey>, new()
    where TUR : class, IUserRole<TUserKey, TRoleKey>, new()
{
    DbSet<TUser> Users { get; set; }
    DbSet<TRole> Roles { get; set; }
    DbSet<TUR> UserRoles { get; set; }
}

public abstract class SdIdentityUnitOfWorkBase<TUserKey, TRoleKey, TUser, TRole, TUR> : SardanapalUnitOfWork
    , ISdIdentityUnitOfWork<TUserKey, TRoleKey, TUser, TRole, TUR>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRole<TRoleKey>, new()
    where TUR : class, IUserRole<TUserKey, TRoleKey>, new()
{
    protected readonly IIdentityHolder _reqClaim;

    public DbSet<TUser> Users { get; set; }
    public DbSet<TRole> Roles { get; set; }
    public DbSet<TUR> UserRoles { get; set; }
    
    public SdIdentityUnitOfWorkBase(DbContextOptions opt, IIdentityHolder requestClaim)
        : base(opt)
    {
        _reqClaim = requestClaim;
    }

    protected override void SetBaseValues()
    {
        var EntityModels = ChangeTracker
            .Entries()
            .Where(e => e.Entity.GetType().IsSubclassOf(typeof(IEntityModel<,>)) && (e.State == EntityState.Added || e.State == EntityState.Modified))
            .ToList();

        foreach (var model in EntityModels)
        {
            var entity = model.Entity;
            var t = entity.GetType();

            if (model.State == EntityState.Added)
            {
                t.GetProperty("CreatedOnUtc")?.SetValue(entity, DateTime.UtcNow);
                t.GetProperty("CreatedBy")?.SetValue(entity, _reqClaim?.Claims?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }
            else if (model.State == EntityState.Modified)
            {
                t.GetProperty("ModifiedOnUtc")?.SetValue(entity, DateTime.UtcNow);
                t.GetProperty("ModifiedBy")?.SetValue(entity, _reqClaim?.Claims?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }
        }

        base.SetBaseValues();
    }
}