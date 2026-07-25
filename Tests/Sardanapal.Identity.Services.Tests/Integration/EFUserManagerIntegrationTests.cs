using System.ComponentModel.DataAnnotations.Schema;
using AutoMapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sardanapal.Contract.IModel;
using Sardanapal.Contract.IService;
using Sardanapal.Ef.Repository;
using Sardanapal.Ef.UnitOfWork;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Repository;
using Sardanapal.Identity.Services.Services.UserManager;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;
using Xunit;

namespace Sardanapal.Identity.Services.Tests.Integration;

public class EfUserDbContext : DbContext, ISdUnitOfWork
{
    public DbSet<IntUser> Users => Set<IntUser>();
    public DbSet<IntUserRole> UserRoles => Set<IntUserRole>();
    public DbSet<IntUserClaim> UserClaims => Set<IntUserClaim>();
    public DbSet<IntClaim> Claims => Set<IntClaim>();

    public EfUserDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<IntUser>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Id).ValueGeneratedOnAdd();
            b.Property(u => u.Username).IsRequired();
            b.Property(u => u.HashedPassword).IsRequired();
        });

        builder.Entity<IntUserRole>(b =>
        {
            b.HasKey(ur => ur.Id);
            b.Property(ur => ur.Id).ValueGeneratedOnAdd();
        });

        builder.Entity<IntUserClaim>(b =>
        {
            b.HasKey(uc => uc.Id);
            b.Property(uc => uc.Id).ValueGeneratedOnAdd();
        });

        builder.Entity<IntClaim>(b =>
        {
            b.HasKey(c => c.Id);
        });

        base.OnModelCreating(builder);
    }

    public Type[] GetDomainModels() => Type.EmptyTypes;

    public void ApplyFluentConfigs<T>(EntityTypeBuilder<T> entity) where T : class, IDomainModel
    {
    }
}

public class EfUserRepository : EFUserRepositoryBase<EfUserDbContext, long, byte, IntUser, IntUserRole, IntUserClaim, IntClaim>
{
    public EfUserRepository(EfUserDbContext context) : base(context)
    {
    }
}

internal sealed class FailingEfUserRepository : EfUserRepository
{
    public FailingEfUserRepository(EfUserDbContext context) : base(context)
    {
    }

    public override Task<long> AddUserRoleAsync(IntUserRole userRole)
    {
        throw new InvalidOperationException("forced failure during role assignment");
    }
}

internal sealed class TestableEFUserManager
    : EFUserManager<EFDatabaseManager<EfUserDbContext>, EfUserRepository, long, IntUser,
        UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM, IntUserRole, IntUserClaim, IntClaim>
{
    public TestableEFUserManager(EFDatabaseManager<EfUserDbContext> dbManager, EfUserRepository repository,
        IMapper mapper, ITokenService tokenService)
        : base(dbManager, repository, mapper, NullLogger.Instance, tokenService)
    {
    }
}

public class EFUserManagerIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public EFUserManagerIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private EfUserDbContext CreateContext()
    {
        DbContextOptions<EfUserDbContext> options = new DbContextOptionsBuilder<EfUserDbContext>()
            .UseSqlite(_connection)
            .Options;
        EfUserDbContext ctx = new EfUserDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static IMapper CreateMapper()
    {
        MapperConfiguration config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<UserEditableVM, IntUser>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.HashedPassword, opt => opt.Ignore());
        });
        return config.CreateMapper();
    }

    [Fact]
    public async Task RegisterUser_EF_Rolls_Back_On_Failure()
    {
        using EfUserDbContext ctx = CreateContext();
        EFDatabaseManager<EfUserDbContext> dbManager = new EFDatabaseManager<EfUserDbContext>(ctx);
        FailingEfUserRepository repo = new FailingEfUserRepository(ctx);
        TestableEFUserManager svc = new TestableEFUserManager(dbManager, repo, CreateMapper(),
            Substitute.For<ITokenService>());
        RegisterVM<byte> model = new RegisterVM<byte>
        {
            Username = "failuser",
            Password = "P4ssword",
            Roles = new List<byte> { 1, 2 }
        };

        IResponse<long> result = await svc.RegisterUser(model);

        result.StatusCode.Should().Be(StatusCode.Exception,
            "a repository failure mid-registration must surface as Exception");

        ctx.Users.AsNoTracking().Should().BeEmpty(
            "the transaction must roll back so the partially-inserted user does not persist");
        ctx.UserRoles.AsNoTracking().Should().BeEmpty(
            "no role assignments must survive the rollback");
    }

    [Fact]
    public async Task RegisterUser_EF_Succeeds_When_No_Failure()
    {
        using EfUserDbContext ctx = CreateContext();
        EFDatabaseManager<EfUserDbContext> dbManager = new EFDatabaseManager<EfUserDbContext>(ctx);
        EfUserRepository repo = new EfUserRepository(ctx);
        TestableEFUserManager svc = new TestableEFUserManager(dbManager, repo, CreateMapper(),
            Substitute.For<ITokenService>());
        RegisterVM<byte> model = new RegisterVM<byte>
        {
            Username = "okuser",
            Password = "P4ssword",
            Roles = new List<byte> { 1, 2 }
        };

        IResponse<long> result = await svc.RegisterUser(model);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        ctx.Users.AsNoTracking().Should().HaveCount(1);
        ctx.Users.AsNoTracking().Single().Username.Should().Be("okuser");
        ctx.UserRoles.AsNoTracking().Should().HaveCount(2);
    }
}
