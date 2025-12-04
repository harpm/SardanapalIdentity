using Microsoft.EntityFrameworkCore;
using Sardanapal.Contract.Data;
using Sardanapal.Contract.IModel;
using Sardanapal.Ef.UnitOfWork;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.Share.Extensions;

namespace Sardanapal.Identity.Domain.Data;

public interface ISdIdentityUnitOfWork<TUserKey, TRoleKey, TClaimKey, TUser, TRole, TClaim, TUR, TUC>
    : ISdUnitOfWork
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TClaimKey : IComparable<TClaimKey>, IEquatable<TClaimKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRole<TRoleKey>, new()
    where TClaim : class, IClaim<TClaimKey>, new()
    where TUR : class, IUserRole<TUserKey, TRoleKey>, new()
    where TUC : class, IUserClaim<TUserKey, TClaimKey>, new()
{
    DbSet<TUser> Users { get; set; }
    DbSet<TRole> Roles { get; set; }
    DbSet<TClaim> Claims { get; set; }
    DbSet<TUR> UserRoles { get; set; }
    DbSet<TUC> UserClaims { get; set; }
}

public abstract class SdIdentityUnitOfWorkBase<TUserKey, TRoleKey, TClaimKey, TUser, TRole, TClaim, TUR, TUC>
    : SardanapalUnitOfWork
    , ISdIdentityUnitOfWork<TUserKey, TRoleKey, TClaimKey, TUser, TRole, TClaim, TUR, TUC>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TClaimKey : IComparable<TClaimKey>, IEquatable<TClaimKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRole<TRoleKey>, new()
    where TClaim : class, IClaim<TClaimKey>, new()
    where TUR : class, IUserRole<TUserKey, TRoleKey>, new()
    where TUC : class, IUserClaim<TUserKey, TClaimKey>, new()
{
    protected readonly IIdentityProvider _reqClaim;

    public DbSet<TUser> Users { get; set; }
    public DbSet<TRole> Roles { get; set; }
    public DbSet<TClaim> Claims { get; set; }
    public DbSet<TUR> UserRoles { get; set; }
    public DbSet<TUC> UserClaims { get; set; }

    public SdIdentityUnitOfWorkBase(DbContextOptions opt, IIdentityProvider requestClaim)
        : base(opt)
    {
        _reqClaim = requestClaim;
    }

    protected abstract TUserKey CreateUserKey(string rawUserKey);

    protected override void SetBaseValues(object? sender, SavingChangesEventArgs e)
    {
        base.SetBaseValues(sender, e);

        var EntityModels = ChangeTracker
            .Entries()
            .Where(e => e.Entity.GetType().ImplementsRawGeneric(typeof(IEntityModel<,>))
                && (e.State == EntityState.Added || e.State == EntityState.Modified))
            .ToList();

        foreach (var model in EntityModels)
        {
            var entity = model.Entity;
            var t = entity.GetType();
            if (_reqClaim?.IsAuthorized ?? false)
            {
                var id = _reqClaim?.Claims?.FindFirst(SdClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    var userId = CreateUserKey(id);
                    if (model.State == EntityState.Added)
                    {
                        t.GetProperty("CreateBy")?.SetValue(entity, userId);
                    }
                    else if (model.State == EntityState.Modified)
                    {
                        t.GetProperty("ModifiedBy")?.SetValue(entity, userId);
                    }
                }
            }

            if (model.State == EntityState.Added)
            {
                t.GetProperty("CreatedOnUtc")?.SetValue(entity, DateTime.UtcNow);
            }
            else if (model.State == EntityState.Modified)
            {
                t.GetProperty("ModifiedOnUtc")?.SetValue(entity, DateTime.UtcNow);
            }

        }
    }
}
