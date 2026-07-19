using System.ComponentModel.DataAnnotations.Schema;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sardanapal.Domain.Model;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Repository;
using Sardanapal.Identity.Services.Services.RoleManager;
using Xunit;

namespace Sardanapal.Identity.Services.Tests.Integration;

public class IntRole : RoleBase<byte>, IRole<byte, long, IntRoleUserRole>
{
    public override byte Id { get; set; }
    public override string Title { get; set; } = string.Empty;

    public virtual ICollection<IntRoleUserRole> UserRoles { get; set; }
        = new HashSet<IntRoleUserRole>();
}

public class IntRoleUserRole : UserRoleBase<long, byte>
{
    public long UserId { get; set; }
    public byte RoleId { get; set; }

    [NotMapped]
    public IntRole? Role { get; set; }
}

public class IntRoleDbContext : DbContext
{
    public DbSet<IntRole> Roles => Set<IntRole>();
    public DbSet<IntRoleUserRole> UserRoles => Set<IntRoleUserRole>();

    public IntRoleDbContext(DbContextOptions<IntRoleDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntRole>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.Title).IsRequired();
            b.HasMany(r => r.UserRoles)
                .WithOne()
                .HasForeignKey(ur => ur.RoleId);
        });

        modelBuilder.Entity<IntRoleUserRole>(b =>
        {
            b.HasKey(ur => ur.Id);
            b.Property(ur => ur.UserId).IsRequired();
            b.Property(ur => ur.RoleId).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}

public class IntRoleRepository : RoleRepositoryBase<IntRoleDbContext, byte, IntRole>, IEFRoleRepository<byte, IntRole>
{
    public IntRoleRepository(IntRoleDbContext context) : base(context)
    {
    }
}

public class IntRoleManager
    : EFRoleManagerBase<IntRoleRepository, long, byte, IntRole, IntRoleUserRole>
{
    public IntRoleManager(IntRoleRepository roleRepository)
        : base(roleRepository, NullLogger.Instance)
    {
    }
}

public class RoleManagerIntegrationTests
{
    private const byte AdminRoleId = 1;
    private const byte EditorRoleId = 2;
    private const long AliceUserId = 10L;
    private const long BobUserId = 20L;

    private static async Task<(IntRoleDbContext ctx, IntRoleManager manager, SqliteConnection conn)>
        CreateContextWithSeedAsync(string testName)
    {
        SqliteConnection connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        DbContextOptions<IntRoleDbContext> options = new DbContextOptionsBuilder<IntRoleDbContext>()
            .UseSqlite(connection)
            .Options;

        IntRoleDbContext ctx = new IntRoleDbContext(options);
        await ctx.Database.EnsureCreatedAsync();

        IntRole adminRole = new IntRole { Id = AdminRoleId, Title = "Admin" };
        IntRole editorRole = new IntRole { Id = EditorRoleId, Title = "Editor" };
        ctx.Roles.AddRange(adminRole, editorRole);

        ctx.UserRoles.Add(new IntRoleUserRole
        {
            Id = 1,
            UserId = AliceUserId,
            RoleId = AdminRoleId
        });

        await ctx.SaveChangesAsync();

        IntRoleRepository repo = new IntRoleRepository(ctx);
        IntRoleManager manager = new IntRoleManager(repo);
        return (ctx, manager, connection);
    }

    [Fact]
    public async Task GetRole_And_GetRoleAsync_Return_Role()
    {
        (IntRoleDbContext ctx, IntRoleManager manager, SqliteConnection conn) setup =
            await CreateContextWithSeedAsync(nameof(GetRole_And_GetRoleAsync_Return_Role));
        await using (setup.ctx)
        await using (setup.conn)
        {
            IntRole sync = setup.manager.GetRole(AdminRoleId);
            IntRole asyncResult = await setup.manager.GetRoleAsync(EditorRoleId);

            sync.Should().NotBeNull();
            sync.Id.Should().Be(AdminRoleId);
            sync.Title.Should().Be("Admin");
            asyncResult.Should().NotBeNull();
            asyncResult.Id.Should().Be(EditorRoleId);
            asyncResult.Title.Should().Be("Editor");
        }
    }

    [Fact]
    public async Task HasRole_And_HasRoleAsync_True_When_Assigned()
    {
        (IntRoleDbContext ctx, IntRoleManager manager, SqliteConnection conn) setup =
            await CreateContextWithSeedAsync(nameof(HasRole_And_HasRoleAsync_True_When_Assigned));
        await using (setup.ctx)
        await using (setup.conn)
        {
            bool sync = setup.manager.HasRole(AdminRoleId, AliceUserId);
            bool asyncResult = await setup.manager.HasRoleAsync(AdminRoleId, AliceUserId);

            sync.Should().BeTrue();
            asyncResult.Should().BeTrue();
        }
    }

    [Fact]
    public async Task HasRole_And_HasRoleAsync_False_When_Not_Assigned()
    {
        (IntRoleDbContext ctx, IntRoleManager manager, SqliteConnection conn) setup =
            await CreateContextWithSeedAsync(nameof(HasRole_And_HasRoleAsync_False_When_Not_Assigned));
        await using (setup.ctx)
        await using (setup.conn)
        {
            bool syncUnassignedRole = setup.manager.HasRole(EditorRoleId, AliceUserId);
            bool asyncUnassignedRole = await setup.manager.HasRoleAsync(EditorRoleId, AliceUserId);
            bool syncUnknownUser = setup.manager.HasRole(AdminRoleId, BobUserId);
            bool asyncUnknownUser = await setup.manager.HasRoleAsync(AdminRoleId, BobUserId);

            syncUnassignedRole.Should().BeFalse();
            asyncUnassignedRole.Should().BeFalse();
            syncUnknownUser.Should().BeFalse();
            asyncUnknownUser.Should().BeFalse();
        }
    }
}
