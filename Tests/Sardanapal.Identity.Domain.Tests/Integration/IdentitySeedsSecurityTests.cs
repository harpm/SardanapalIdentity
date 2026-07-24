using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Domain;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Share.Static;
using Xunit;

namespace Sardanapal.Identity.Domain.Tests.Integration;

public class IdentitySeedsSecurityTests
{
    public enum SecRoles { Admin = 1, Editor = 2 }

    public class SecUser : UserBase<long>, IUser<long> { }
    public class SecUserRole : UserRoleBase<long, byte> { }
    public class SecRole : RoleBase<byte>, IRoleBase<byte> { }

    public class SecDbContext : DbContext
    {
        public DbSet<SecUser> Users => Set<SecUser>();
        public DbSet<SecRole> Roles => Set<SecRole>();
        public DbSet<SecUserRole> UserRoles => Set<SecUserRole>();

        public SecDbContext(DbContextOptions<SecDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SecUser>(b =>
            {
                b.HasKey(u => u.Id);
                b.Property(u => u.Username).IsRequired();
                b.Property(u => u.HashedPassword).IsRequired();
            });
            modelBuilder.Entity<SecRole>(b =>
            {
                b.HasKey(r => r.Id);
                b.Property(r => r.Title).IsRequired();
            });
            modelBuilder.Entity<SecUserRole>(b =>
            {
                b.HasKey(ur => ur.Id);
                b.Property(ur => ur.UserId).IsRequired();
                b.Property(ur => ur.RoleId).IsRequired();
            });
            base.OnModelCreating(modelBuilder);
        }
    }

    private static ServiceProvider BuildProvider(string name, SDConfigs config)
    {
        ServiceCollection services = new ServiceCollection();
        SqliteConnection connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        services.AddDbContext<SecDbContext>(o => o.UseSqlite(connection));
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<SecDbContext>());
        services.AddSingleton(connection);
        services.AddSingleton<IOptions<SDConfigs>>(Options.Create(config));
        ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<SecDbContext>().Database.EnsureCreatedAsync().GetAwaiter().GetResult();
        return provider;
    }

    [Theory]
    [InlineData("ExplicitPw!1")]
    [InlineData(null)]
    public async Task Admin_Seed_Always_Sets_MustChangePassword_True(string? configuredPassword)
    {
        SDConfigs config = new SDConfigs { SeedAdminPassword = configuredPassword };
        ServiceProvider provider = BuildProvider(nameof(Admin_Seed_Always_Sets_MustChangePassword_True) + configuredPassword, config);

        await provider.AddAdminUser<SecRoles, SecUser, SecUserRole, long, byte>();

        using (provider)
        using (SecDbContext ctx = provider.CreateScope().ServiceProvider.GetRequiredService<SecDbContext>())
        {
            SecUser admin = ctx.Users.AsNoTracking().Single();
            admin.MustChangePassword.Should().BeTrue(
                "a freshly seeded admin must always be forced to change the generated or provided password");

            if (!string.IsNullOrEmpty(configuredPassword))
            {
                admin.HashedPassword.Should().NotBe(configuredPassword, "the configured password must never be stored in plaintext");
                Utilities.VerifyPassword(configuredPassword, admin.HashedPassword).Should().BeTrue();
            }
            else
            {
                admin.HashedPassword.Should().NotBeNullOrWhiteSpace("a generated password must be hashed and stored");
                Utilities.VerifyPassword("__sardanapal_dummy_do_not_match__", admin.HashedPassword).Should().BeFalse(
                    "a generated password must hash to something unguessable");
            }
        }

        provider.Dispose();
    }
}
