using System.ComponentModel.DataAnnotations.Schema;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Domain;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Share.Static;
using Xunit;

namespace Sardanapal.Identity.Domain.Tests.Integration;

public enum TestRoles
{
    Admin = 1,
    Editor = 2,
    Viewer = 3
}

public class SeedUser : UserBase<long>, IUser<long>
{
}

public class SeedUserRole : UserRoleBase<long, byte>
{
}

public class SeedRole : RoleBase<byte>, IRoleBase<byte>
{
}

public class SeedDbContext : DbContext
{
    public DbSet<SeedUser> Users => Set<SeedUser>();
    public DbSet<SeedRole> Roles => Set<SeedRole>();
    public DbSet<SeedUserRole> UserRoles => Set<SeedUserRole>();

    public SeedDbContext(DbContextOptions<SeedDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SeedUser>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Username).IsRequired();
            b.Property(u => u.HashedPassword).IsRequired();
            b.Property(u => u.Email).IsRequired(false);
        });

        modelBuilder.Entity<SeedRole>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.Title).IsRequired();
        });

        modelBuilder.Entity<SeedUserRole>(b =>
        {
            b.HasKey(ur => ur.Id);
            b.Property(ur => ur.UserId).IsRequired();
            b.Property(ur => ur.RoleId).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}

public class IdentitySeedsIntegrationTests
{
    private static ServiceProvider BuildProvider(string dbName, Action<ServiceCollection>? configure = null, SDConfigs? config = null)
    {
        ServiceCollection services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        SqliteConnection connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        services.AddDbContext<SeedDbContext>(o => o.UseSqlite(connection));

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<SeedDbContext>());

        services.AddSingleton(connection);

        if (config != null)
        {
            services.AddSingleton<IOptions<SDConfigs>>(Options.Create(config));
        }

        configure?.Invoke(services);

        ServiceProvider provider = services.BuildServiceProvider();

        using (IServiceScope scope = provider.CreateScope())
        {
            SeedDbContext ctx = scope.ServiceProvider.GetRequiredService<SeedDbContext>();
            ctx.Database.EnsureCreatedAsync().GetAwaiter().GetResult();
        }

        return provider;
    }

    private static SeedDbContext Resolve(ServiceProvider provider)
    {
        return provider.CreateScope().ServiceProvider.GetRequiredService<SeedDbContext>();
    }

    [Fact]
    public void AddRoles_Seeds_One_Row_Per_Enum_Value_With_Title()
    {
        ServiceProvider provider = BuildProvider(nameof(AddRoles_Seeds_One_Row_Per_Enum_Value_With_Title));

        provider.AddRoles<TestRoles, SeedRole, byte>();

        using (provider)
        using (SeedDbContext ctx = Resolve(provider))
        {
            List<SeedRole> roles = ctx.Roles.AsNoTracking().OrderBy(r => r.Id).ToList();
            roles.Should().HaveCount(3);
            roles.Select(r => r.Id).Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
            roles.Select(r => r.Title).Should().BeEquivalentTo(new[] { "Admin", "Editor", "Viewer" });
        }

        provider.Dispose();
    }

    [Fact]
    public void AddRoles_Is_Idempotent()
    {
        ServiceProvider provider = BuildProvider(nameof(AddRoles_Is_Idempotent));

        provider.AddRoles<TestRoles, SeedRole, byte>();
        provider.AddRoles<TestRoles, SeedRole, byte>();

        using (provider)
        using (SeedDbContext ctx = Resolve(provider))
        {
            ctx.Roles.AsNoTracking().Should().HaveCount(3);
        }

        provider.Dispose();
    }

    [Fact]
    public void AddAdminUser_Uses_Config_Username_Or_Default_Admin()
    {
        ServiceProvider providerWithConfig = BuildProvider("cfg",
            config: new SDConfigs { SeedAdminUsername = "root" });

        providerWithConfig.AddAdminUser<TestRoles, SeedUser, SeedUserRole, long, byte>().GetAwaiter().GetResult();

        using (providerWithConfig)
        using (SeedDbContext ctx = Resolve(providerWithConfig))
        {
            SeedUser? admin = ctx.Users.AsNoTracking().FirstOrDefault(u => u.Username == "root");
            admin.Should().NotBeNull();
        }

        providerWithConfig.Dispose();

        ServiceProvider providerWithoutConfig = BuildProvider("default");

        providerWithoutConfig.AddAdminUser<TestRoles, SeedUser, SeedUserRole, long, byte>().GetAwaiter().GetResult();

        using (providerWithoutConfig)
        using (SeedDbContext ctx = Resolve(providerWithoutConfig))
        {
            SeedUser? admin = ctx.Users.AsNoTracking().FirstOrDefault(u => u.Username == "admin");
            admin.Should().NotBeNull();
        }

        providerWithoutConfig.Dispose();
    }

    [Fact]
    public void AddAdminUser_Uses_Config_Password_When_Provided()
    {
        const string configuredPassword = "ConfiguredStrong!1";
        ServiceProvider provider = BuildProvider(nameof(AddAdminUser_Uses_Config_Password_When_Provided),
            config: new SDConfigs { SeedAdminPassword = configuredPassword });

        provider.AddAdminUser<TestRoles, SeedUser, SeedUserRole, long, byte>().GetAwaiter().GetResult();

        using (provider)
        using (SeedDbContext ctx = Resolve(provider))
        {
            SeedUser admin = ctx.Users.AsNoTracking().Single();
            admin.HashedPassword.Should().NotBe(configuredPassword);
            admin.HashedPassword.Should().NotBeNullOrWhiteSpace();
            Utilities.VerifyPassword(configuredPassword, admin.HashedPassword).Should().BeTrue();
        }

        provider.Dispose();
    }

    [Fact]
    public void AddAdminUser_Generates_Strong_Password_And_Warns_When_Not_Provided()
    {
        CapturingLoggerProvider loggerProvider = new CapturingLoggerProvider();
        ServiceProvider provider = BuildProvider(nameof(AddAdminUser_Generates_Strong_Password_And_Warns_When_Not_Provided),
            configure: s => s.AddSingleton<ILoggerProvider>(loggerProvider));

        provider.AddAdminUser<TestRoles, SeedUser, SeedUserRole, long, byte>().GetAwaiter().GetResult();

        using (provider)
        using (SeedDbContext ctx = Resolve(provider))
        {
            SeedUser admin = ctx.Users.AsNoTracking().Single();
            admin.HashedPassword.Should().NotBeNullOrWhiteSpace();
            loggerProvider.WarningRecords.Should().NotBeEmpty();
            loggerProvider.WarningRecords.Should().Contain(r => r.Message.Contains("admin", StringComparison.OrdinalIgnoreCase));
        }

        provider.Dispose();
    }

    [Fact]
    public void AddAdminUser_Sets_MustChangePassword_True()
    {
        ServiceProvider provider = BuildProvider(nameof(AddAdminUser_Sets_MustChangePassword_True),
            config: new SDConfigs { SeedAdminPassword = "ProvidedPw!1" });

        provider.AddAdminUser<TestRoles, SeedUser, SeedUserRole, long, byte>().GetAwaiter().GetResult();

        using (provider)
        using (SeedDbContext ctx = Resolve(provider))
        {
            SeedUser admin = ctx.Users.AsNoTracking().Single();
            admin.MustChangePassword.Should().BeTrue();
        }

        provider.Dispose();
    }

    [Fact]
    public void AddAdminUser_Sets_VerifiedEmail_And_VerifiedPhoneNumber_True()
    {
        ServiceProvider provider = BuildProvider(nameof(AddAdminUser_Sets_VerifiedEmail_And_VerifiedPhoneNumber_True),
            config: new SDConfigs { SeedAdminPassword = "ProvidedPw!1" });

        provider.AddAdminUser<TestRoles, SeedUser, SeedUserRole, long, byte>().GetAwaiter().GetResult();

        using (provider)
        using (SeedDbContext ctx = Resolve(provider))
        {
            SeedUser admin = ctx.Users.AsNoTracking().Single();
            admin.VerifiedEmail.Should().BeTrue();
            admin.VerifiedPhoneNumber.Should().BeTrue();
        }

        provider.Dispose();
    }

    [Fact]
    public void AddAdminUser_Assigns_All_Roles_From_Enum()
    {
        ServiceProvider provider = BuildProvider(nameof(AddAdminUser_Assigns_All_Roles_From_Enum),
            config: new SDConfigs { SeedAdminPassword = "ProvidedPw!1" });

        provider.AddAdminUser<TestRoles, SeedUser, SeedUserRole, long, byte>().GetAwaiter().GetResult();

        using (provider)
        using (SeedDbContext ctx = Resolve(provider))
        {
            SeedUser admin = ctx.Users.AsNoTracking().Single();
            List<byte> assignedRoles = ctx.UserRoles.AsNoTracking()
                .Where(ur => ur.UserId == admin.Id)
                .Select(ur => ur.RoleId)
                .OrderBy(r => r)
                .ToList();
            assignedRoles.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
        }

        provider.Dispose();
    }

    [Fact]
    public void AddAdminUser_Is_Idempotent()
    {
        ServiceProvider provider = BuildProvider(nameof(AddAdminUser_Is_Idempotent),
            config: new SDConfigs { SeedAdminPassword = "ProvidedPw!1" });

        provider.AddAdminUser<TestRoles, SeedUser, SeedUserRole, long, byte>().GetAwaiter().GetResult();
        provider.AddAdminUser<TestRoles, SeedUser, SeedUserRole, long, byte>().GetAwaiter().GetResult();

        using (provider)
        using (SeedDbContext ctx = Resolve(provider))
        {
            ctx.Users.AsNoTracking().Where(u => u.Username == "admin").Should().HaveCount(1);
            SeedUser admin = ctx.Users.AsNoTracking().Single(u => u.Username == "admin");
            List<SeedUserRole> userRoles = ctx.UserRoles.AsNoTracking().Where(ur => ur.UserId == admin.Id).ToList();
            userRoles.Should().HaveCount(3);
        }

        provider.Dispose();
    }

    [Fact]
    public void AddAdminUser_Null_DbContext_Throws_NullReferenceException()
    {
        ServiceCollection services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();

        Action act = () => provider.AddAdminUser<TestRoles, SeedUser, SeedUserRole, long, byte>().GetAwaiter().GetResult();

        act.Should().Throw<NullReferenceException>();
        provider.Dispose();
    }
}

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public List<(string CategoryName, LogLevel Level, string Message)> WarningRecords { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly CapturingLoggerProvider _owner;
        private readonly string _categoryName;

        public CapturingLogger(CapturingLoggerProvider owner, string categoryName)
        {
            _owner = owner;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
            {
                _owner.WarningRecords.Add((_categoryName, logLevel, formatter(state, exception)));
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new NullScope();
        public void Dispose() { }
    }
}
