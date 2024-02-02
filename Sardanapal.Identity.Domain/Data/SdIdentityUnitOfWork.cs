using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.ModelBase.UnitOfWork;

namespace Sardanapal.Identity.Domain.Data
{
    public class SdIdentityUnitOfWorkBase<TKey> : DbContext, IUnitOfWork
        where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        public DbSet<UserBase<TKey>> Users { get; set; }
        public DbSet<RoleBase<byte>> Roles { get; set; }
        public DbSet<UserRole<TKey>> UserRoles { get; set; }
    }

    public class SdIdentityUnitOfWork<TKey> : SdIdentityUnitOfWorkBase<TKey>, IUnitOfWork
        where TKey : IComparable<TKey>, IEquatable<TKey>
    {

    }
}
