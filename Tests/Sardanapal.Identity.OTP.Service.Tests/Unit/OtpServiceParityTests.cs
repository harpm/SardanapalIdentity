using AutoMapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sardanapal.Contract.IModel;
using Sardanapal.Contract.IService;
using Sardanapal.Contract.IRepository;
using Sardanapal.Ef.Repository;
using Sardanapal.Ef.UnitOfWork;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.OTP.Domain;
using Sardanapal.Identity.OTP.Services;
using Sardanapal.Identity.Repository;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Response;
using Xunit;

namespace Sardanapal.Identity.OTP.Service.Tests.Unit;

public sealed class EfOtpModel : OTPModel<long, Guid>
{
}

public sealed class EfOtpDbContext : DbContext, ISdUnitOfWork
{
    public DbSet<EfOtpModel> Otps => Set<EfOtpModel>();

    public EfOtpDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<EfOtpModel>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedOnAdd();
            b.Property(x => x.Code).IsRequired();
            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.ExpireTime).IsRequired();
        });

        base.OnModelCreating(builder);
    }

    public Type[] GetDomainModels() => Type.EmptyTypes;

    public void ApplyFluentConfigs<T>(EntityTypeBuilder<T> entity) where T : class, IDomainModel
    {
    }
}

public sealed class EfOtpRepository : EFOTPRepositoryBase<EfOtpDbContext, Guid, EfOtpModel>
{
    public EfOtpRepository(EfOtpDbContext context) : base(context)
    {
    }
}

public sealed class MemoryOtpRepository : OTPRepositoryBase<Guid, EfOtpModel>
{
    public override void Add(EfOtpModel model, CancellationToken ct = default)
    {
        if (model.Id == Guid.Empty) model.Id = Guid.NewGuid();
        base.Add(model, ct);
    }

    public override Task AddAsync(EfOtpModel model, CancellationToken ct = default)
    {
        if (model.Id == Guid.Empty) model.Id = Guid.NewGuid();
        return base.AddAsync(model, ct);
    }
}

public class OtpServiceParityTests
{
    private const long UserId = 7L;
    private const byte RoleId = 2;
    private const string Recipient = "alice@example.com";
    private const string FixedCode = "424242";

    private static IMapper CreateMapper()
    {
        MapperConfiguration config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NewOtpVM<long>, EfOtpModel>();
            cfg.CreateMap<EfOtpModel, OtpVM<long>>();
        });
        return config.CreateMapper();
    }

    private sealed class SharedDeps
    {
        public IMapper Mapper { get; } = CreateMapper();
        public IRequestService Request { get; } = Substitute.For<IRequestService>();
        public IEmailService Email { get; } = Substitute.For<IEmailService>();
        public ISmsService Sms { get; } = Substitute.For<ISmsService>();
        public IOtpHelper Helper { get; } = Substitute.For<IOtpHelper>();

        public SharedDeps()
        {
            Helper.GenerateNewOtp().Returns(FixedCode);
        }
    }

    private sealed class EfVariant : IDisposable
    {
        public SqliteConnection Connection { get; }
        public EfOtpDbContext Context { get; }
        public EfOtpRepository Repo { get; }
        public EFOtpService<EFDatabaseManager<EfOtpDbContext>, EfOtpRepository, long, Guid, EfOtpModel, OtpListItemVM<Guid>, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>>
            Service { get; }

        public EfVariant(SharedDeps deps, int expireMinutes)
        {
            Connection = new SqliteConnection("DataSource=:memory:");
            Connection.Open();
            DbContextOptions<EfOtpDbContext> options = new DbContextOptionsBuilder<EfOtpDbContext>()
                .UseSqlite(Connection)
                .Options;
            Context = new EfOtpDbContext(options);
            Context.Database.EnsureCreated();

            Repo = new EfOtpRepository(Context);
            EFDatabaseManager<EfOtpDbContext> dbManager = new EFDatabaseManager<EfOtpDbContext>(Context);

            Service = new EFOtpService<EFDatabaseManager<EfOtpDbContext>, EfOtpRepository, long, Guid, EfOtpModel, OtpListItemVM<Guid>, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>>
                (dbManager, Repo, deps.Mapper, deps.Request, deps.Email, deps.Sms, deps.Helper, NullLogger.Instance);
            Service.expireTime = expireMinutes;
        }

        public void Seed(EfOtpModel model)
        {
            Context.Otps.Add(model);
            Context.SaveChanges();
        }

        public int Count() => Context.Otps.AsNoTracking().Count();

        public void Dispose()
        {
            Context.Dispose();
            Connection.Dispose();
        }
    }

    private sealed class MemoryVariant
    {
        public MemoryOtpRepository Repo { get; }
        public OtpService<MemoryOtpRepository, long, Guid, EfOtpModel, OtpListItemVM<Guid>, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>>
            Service { get; }

        public MemoryVariant(SharedDeps deps, int expireMinutes)
        {
            Repo = new MemoryOtpRepository();
            Service = new OtpService<MemoryOtpRepository, long, Guid, EfOtpModel, OtpListItemVM<Guid>, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>>
                (Repo, deps.Mapper, deps.Request, deps.Email, deps.Sms, deps.Helper, NullLogger.Instance);
            Service.expireTime = expireMinutes;
        }

        public void Seed(EfOtpModel model)
        {
            if (model.Id == Guid.Empty) model.Id = Guid.NewGuid();
            Repo.Add(model);
        }

        public int Count() => Repo.FetchAll().Count();
    }

    private static NewOtpVM<long> NewModel() => new NewOtpVM<long>
    {
        UserId = UserId,
        RoleId = RoleId,
        Recipient = Recipient,
        Username = "alice"
    };

    private static EfOtpModel Existing(string code, DateTime expire) => new EfOtpModel
    {
        Id = Guid.NewGuid(),
        Code = code,
        UserId = UserId,
        RoleId = RoleId,
        ExpireTime = expire
    };

    [Fact]
    public void EF_And_Memory_Variants_Produce_Same_Outcomes_For_Same_Inputs()
    {
        typeof(EFOtpService<,,,,,,,,>).GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IOtpService<,,,,>))
            .Should().BeTrue("EFOtpService must implement IOtpService");
        typeof(OtpService<,,,,,,,>).GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IOtpService<,,,,>))
            .Should().BeTrue("OtpService must implement IOtpService");
    }

    [Fact]
    public async Task Add_When_Empty_Both_Variants_Succeed_With_NonEmpty_Id()
    {
        SharedDeps deps = new SharedDeps();
        const int expireMinutes = 5;
        using EfVariant ef = new EfVariant(deps, expireMinutes);
        MemoryVariant mem = new MemoryVariant(deps, expireMinutes);
        NewOtpVM<long> model = NewModel();

        IResponse<Guid> efRes = await ef.Service.Add(model);
        IResponse<Guid> memRes = await mem.Service.Add(model);

        efRes.StatusCode.Should().Be(memRes.StatusCode);
        efRes.StatusCode.Should().Be(StatusCode.Succeeded);
        efRes.Data.Should().NotBeEmpty();
        memRes.Data.Should().NotBeEmpty();
        ef.Count().Should().Be(mem.Count()).And.Be(1);
    }

    [Fact]
    public async Task Add_When_Active_Otp_Both_Variants_Return_Canceled_OtpCooldown()
    {
        SharedDeps deps = new SharedDeps();
        const int expireMinutes = 6;
        using EfVariant ef = new EfVariant(deps, expireMinutes);
        MemoryVariant mem = new MemoryVariant(deps, expireMinutes);

        ef.Seed(Existing("1111", DateTime.UtcNow.AddMinutes(2)));
        mem.Seed(Existing("1111", DateTime.UtcNow.AddMinutes(2)));

        IResponse<Guid> efRes = await ef.Service.Add(NewModel());
        IResponse<Guid> memRes = await mem.Service.Add(NewModel());

        efRes.StatusCode.Should().Be(memRes.StatusCode).And.Be(StatusCode.Canceled);
        efRes.UserMessage.Should().Be(memRes.UserMessage);
        efRes.UserMessage.Should().Be(string.Format(Identity_Messages.OtpCooldown, expireMinutes));
        ef.Count().Should().Be(mem.Count()).And.Be(1);
    }

    [Fact]
    public async Task ValidateCode_Match_Both_Variants_Succeed_With_Same_Code_And_Delete()
    {
        SharedDeps deps = new SharedDeps();
        const int expireMinutes = 5;
        using EfVariant ef = new EfVariant(deps, expireMinutes);
        MemoryVariant mem = new MemoryVariant(deps, expireMinutes);
        ef.Seed(Existing(FixedCode, DateTime.UtcNow.AddMinutes(3)));
        mem.Seed(Existing(FixedCode, DateTime.UtcNow.AddMinutes(3)));
        NewOtpVM<long> probe = new NewOtpVM<long> { UserId = UserId, RoleId = RoleId, Code = FixedCode };

        IResponse<OtpVM<long>> efRes = await ef.Service.ValidateCode(probe);
        IResponse<OtpVM<long>> memRes = await mem.Service.ValidateCode(probe);

        efRes.StatusCode.Should().Be(memRes.StatusCode).And.Be(StatusCode.Succeeded);
        efRes.Data.Code.Should().Be(memRes.Data.Code).And.Be(FixedCode);
        ef.Count().Should().Be(mem.Count()).And.Be(0, "a matched OTP must be deleted");

        IResponse<OtpVM<long>> efReplay = await ef.Service.ValidateCode(probe);
        IResponse<OtpVM<long>> memReplay = await mem.Service.ValidateCode(probe);
        efReplay.StatusCode.Should().Be(memReplay.StatusCode).And.Be(StatusCode.NotExists);
    }

    [Fact]
    public async Task ValidateCode_Expired_Both_Variants_Return_OtpCodeExpired_No_Delete()
    {
        SharedDeps deps = new SharedDeps();
        const int expireMinutes = 5;
        using EfVariant ef = new EfVariant(deps, expireMinutes);
        MemoryVariant mem = new MemoryVariant(deps, expireMinutes);
        ef.Seed(Existing(FixedCode, DateTime.UtcNow.AddMinutes(-1)));
        mem.Seed(Existing(FixedCode, DateTime.UtcNow.AddMinutes(-1)));
        NewOtpVM<long> probe = new NewOtpVM<long> { UserId = UserId, RoleId = RoleId, Code = FixedCode };

        IResponse<OtpVM<long>> efRes = await ef.Service.ValidateCode(probe);
        IResponse<OtpVM<long>> memRes = await mem.Service.ValidateCode(probe);

        efRes.StatusCode.Should().Be(memRes.StatusCode).And.Be(StatusCode.Failed);
        efRes.UserMessage.Should().Be(memRes.UserMessage).And.Be(Identity_Messages.OtpCodeExpired);
        ef.Count().Should().Be(mem.Count()).And.Be(1, "an expired OTP must not be deleted");
    }

    [Fact]
    public async Task ValidateCode_NoMatch_Both_Variants_Return_NotExists()
    {
        SharedDeps deps = new SharedDeps();
        const int expireMinutes = 5;
        using EfVariant ef = new EfVariant(deps, expireMinutes);
        MemoryVariant mem = new MemoryVariant(deps, expireMinutes);
        ef.Seed(Existing(FixedCode, DateTime.UtcNow.AddMinutes(3)));
        mem.Seed(Existing(FixedCode, DateTime.UtcNow.AddMinutes(3)));
        NewOtpVM<long> probe = new NewOtpVM<long> { UserId = UserId, RoleId = RoleId, Code = "0000" };

        IResponse<OtpVM<long>> efRes = await ef.Service.ValidateCode(probe);
        IResponse<OtpVM<long>> memRes = await mem.Service.ValidateCode(probe);

        efRes.StatusCode.Should().Be(memRes.StatusCode).And.Be(StatusCode.NotExists);
        ef.Count().Should().Be(mem.Count()).And.Be(1);
    }
}
