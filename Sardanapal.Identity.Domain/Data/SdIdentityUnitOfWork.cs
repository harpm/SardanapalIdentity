using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.DomainModel.UnitOfWork;
using Sardanapal.DomainModel.Domain;
using Sardanapal.Identity.Authorization.Data;
using System.Security.Claims;

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
    protected readonly RequestClaim _reqClaim;

    public DbSet<TUser> Users { get; set; }
    public DbSet<TRole> Roles { get; set; }
    public DbSet<TUR> UserRoles { get; set; }
    
    public SdIdentityUnitOfWorkBase(RequestClaim requestClaim)
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