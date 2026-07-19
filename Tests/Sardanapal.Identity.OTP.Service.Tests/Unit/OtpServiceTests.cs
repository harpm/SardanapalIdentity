using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sardanapal.Contract.IModel;
using Sardanapal.Contract.IRepository;
using Sardanapal.Contract.IService;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.OTP.Domain;
using Sardanapal.Identity.OTP.Services;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Models;
using Sardanapal.ViewModel.Response;
using Xunit;

namespace Sardanapal.Identity.OTP.Service.Tests.Unit;

public sealed class TestOtpModel : OTPModel<long, Guid>
{
}

public interface ITestOtpRepository : IOTPRepository<Guid, TestOtpModel>, IMemoryRepository<Guid, TestOtpModel>
{
}

public class OtpServiceTests
{
    private const long UserId = 7L;
    private const byte RoleId = 2;
    private const string EmailRecipient = "alice@example.com";
    private const string SmsRecipient = "9876543210";

    private static readonly Guid AssignedId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static IMapper CreateMapper()
    {
        MapperConfiguration config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NewOtpVM<long>, TestOtpModel>();
            cfg.CreateMap<TestOtpModel, OtpVM<long>>();
            cfg.CreateMap<TestOtpModel, OtpListItemVM<Guid>>();
        });
        return config.CreateMapper();
    }

    private sealed class Deps
    {
        public ITestOtpRepository Repo { get; } = Substitute.For<ITestOtpRepository>();
        public IMapper Mapper { get; } = CreateMapper();
        public IRequestService Request { get; } = Substitute.For<IRequestService>();
        public IEmailService Email { get; } = Substitute.For<IEmailService>();
        public ISmsService Sms { get; } = Substitute.For<ISmsService>();
        public IOtpHelper Helper { get; } = Substitute.For<IOtpHelper>();

        public OtpService<ITestOtpRepository, long, Guid, TestOtpModel, OtpListItemVM<Guid>, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>>
            BuildService(int expireMinutes)
        {
            OtpService<ITestOtpRepository, long, Guid, TestOtpModel, OtpListItemVM<Guid>, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> svc =
                new(Repo, Mapper, Request, Email, Sms, Helper, NullLogger.Instance);
            svc.expireTime = expireMinutes;
            return svc;
        }

        public NewOtpVM<long> NewModel(string recipient) => new NewOtpVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Recipient = recipient,
            Username = "alice"
        };
    }

    private static TestOtpModel ExistingOtp(string code, DateTime expireTime, Guid? id = null) => new TestOtpModel
    {
        Id = id ?? Guid.NewGuid(),
        Code = code,
        UserId = UserId,
        RoleId = RoleId,
        ExpireTime = expireTime
    };

    private static void SetupRepoReturns(ITestOtpRepository repo, params TestOtpModel[] items)
    {
        List<TestOtpModel> list = new List<TestOtpModel>(items);
        repo.FetchAll(Arg.Any<CancellationToken>()).Returns(list);
        repo.FetchAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<TestOtpModel>>(list));
    }

    [Fact]
    public async Task Add_Cooldown_When_Active_Otp_For_User_Role_Returns_Canceled_OtpCooldown()
    {
        Deps d = new Deps();
        SetupRepoReturns(d.Repo, ExistingOtp("1111", DateTime.UtcNow.AddMinutes(2)));
        d.Helper.GenerateNewOtp().Returns("2222");
        var svc = d.BuildService(expireMinutes: 5);

        IResponse<Guid> result = await svc.Add(d.NewModel(EmailRecipient));

        result.StatusCode.Should().Be(StatusCode.Canceled);
        result.UserMessage.Should().Be(string.Format(Identity_Messages.OtpCooldown, 5));
        await d.Repo.DidNotReceive().AddAsync(Arg.Any<TestOtpModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_No_Existing_Sets_ExpireTime_To_Now_Plus_ExpireMinutes()
    {
        Deps d = new Deps();
        SetupRepoReturns(d.Repo);
        d.Helper.GenerateNewOtp().Returns("3333");
        TestOtpModel? captured = null;
        d.Repo.AddAsync(Arg.Do<TestOtpModel>(m => { m.Id = AssignedId; captured = m; }), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        const int expireMinutes = 8;
        var svc = d.BuildService(expireMinutes);
        DateTime before = DateTime.UtcNow;

        await svc.Add(d.NewModel(EmailRecipient));

        DateTime after = DateTime.UtcNow;
        captured.Should().NotBeNull();
        captured!.ExpireTime.Should().BeCloseTo(before.AddMinutes(expireMinutes), TimeSpan.FromSeconds(5));
        captured.ExpireTime.Should().BeAfter(after.AddMinutes(expireMinutes).AddSeconds(-5));
    }

    [Fact]
    public async Task Add_Generates_Code_Via_OtpHelper()
    {
        Deps d = new Deps();
        SetupRepoReturns(d.Repo);
        const string code = "999999";
        d.Helper.GenerateNewOtp().Returns(code);
        TestOtpModel? captured = null;
        d.Repo.AddAsync(Arg.Do<TestOtpModel>(m => { m.Id = AssignedId; captured = m; }), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var svc = d.BuildService(5);

        await svc.Add(d.NewModel(EmailRecipient));

        captured.Should().NotBeNull();
        captured!.Code.Should().Be(code);
        d.Helper.Received(1).GenerateNewOtp();
    }

    [Fact]
    public async Task Add_Numeric_Recipient_Routes_To_Sms()
    {
        Deps d = new Deps();
        SetupRepoReturns(d.Repo);
        d.Helper.GenerateNewOtp().Returns("1234");
        d.Repo.AddAsync(Arg.Do<TestOtpModel>(m => m.Id = AssignedId), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var svc = d.BuildService(5);

        await svc.Add(d.NewModel(SmsRecipient));

        d.Sms.Received(1).Send(SmsRecipient, "1234", Arg.Any<CancellationToken>());
        d.Email.DidNotReceiveWithAnyArgs().Send(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_NonNumeric_Recipient_Routes_To_Email()
    {
        Deps d = new Deps();
        SetupRepoReturns(d.Repo);
        d.Helper.GenerateNewOtp().Returns("4321");
        d.Repo.AddAsync(Arg.Do<TestOtpModel>(m => m.Id = AssignedId), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var svc = d.BuildService(5);

        await svc.Add(d.NewModel(EmailRecipient));

        d.Email.Received(1).Send(EmailRecipient, "4321", Arg.Any<CancellationToken>());
        d.Sms.DidNotReceiveWithAnyArgs().Send(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_Returns_New_Id()
    {
        Deps d = new Deps();
        SetupRepoReturns(d.Repo);
        d.Helper.GenerateNewOtp().Returns("5555");
        d.Repo.AddAsync(Arg.Do<TestOtpModel>(m => m.Id = AssignedId), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var svc = d.BuildService(5);

        IResponse<Guid> result = await svc.Add(d.NewModel(EmailRecipient));

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().Be(AssignedId);
    }

    [Fact]
    public async Task ValidateCode_Match_Not_Expired_Succeeds_And_Deletes_Record()
    {
        Guid otpId = Guid.NewGuid();
        Deps d = new Deps();
        SetupRepoReturns(d.Repo, ExistingOtp("7777", DateTime.UtcNow.AddMinutes(3), otpId));
        var svc = d.BuildService(5);

        IResponse<OtpVM<long>> result = await svc.ValidateCode(new NewOtpVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "7777"
        });

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().NotBeNull();
        result.Data.Code.Should().Be("7777");
        await d.Repo.Received(1).DeleteAsync(otpId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateCode_Match_Expired_Returns_Failed_OtpCodeExpired()
    {
        Guid otpId = Guid.NewGuid();
        Deps d = new Deps();
        SetupRepoReturns(d.Repo, ExistingOtp("8888", DateTime.UtcNow.AddMinutes(-1), otpId));
        var svc = d.BuildService(5);

        IResponse<OtpVM<long>> result = await svc.ValidateCode(new NewOtpVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "8888"
        });

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(Identity_Messages.OtpCodeExpired);
        await d.Repo.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateCode_No_Match_Returns_NotExists()
    {
        Deps d = new Deps();
        SetupRepoReturns(d.Repo, ExistingOtp("0000", DateTime.UtcNow.AddMinutes(3)));
        var svc = d.BuildService(5);

        IResponse<OtpVM<long>> result = await svc.ValidateCode(new NewOtpVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "9999"
        });

        result.StatusCode.Should().Be(StatusCode.NotExists);
        await d.Repo.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_Returns_Paged_List()
    {
        Deps d = new Deps();
        TestOtpModel a = ExistingOtp("1111", DateTime.UtcNow.AddMinutes(2));
        TestOtpModel b = ExistingOtp("2222", DateTime.UtcNow.AddMinutes(2));
        SetupRepoReturns(d.Repo, a, b);
        var svc = d.BuildService(5);

        IResponse<GridVM<Guid, OtpVM<long>>> result =
            await svc.GetAll<OtpVM<long>>();

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().NotBeNull();
        result.Data.List.Should().NotBeNull();
        result.Data.List.Should().HaveCount(2);
        result.Data.List.Select(x => x.Code).Should().BeEquivalentTo(new[] { "1111", "2222" });
    }

    [Fact]
    public void EF_And_Memory_Variants_Produce_Same_Outcomes_For_Same_Inputs()
    {
        Type efType = typeof(EFOtpService<,,,,,,,,>);
        Type memType = typeof(OtpService<,,,,,,,>);

        ImplementsOpenGeneric(efType, typeof(IOtpService<,,,,>)).Should().BeTrue();
        ImplementsOpenGeneric(memType, typeof(IOtpService<,,,,>)).Should().BeTrue();

        HashSet<string> efMethods = PublicMethodSignatures(efType);
        HashSet<string> memMethods = PublicMethodSignatures(memType);

        foreach (string key in new[] { "Add", "ValidateCode", "GetAll" })
        {
            memMethods.Should().Contain(key);
            efMethods.Should().Contain(key);
        }
    }

    private static bool ImplementsOpenGeneric(Type type, Type openGenericInterface)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);
    }

    private static HashSet<string> PublicMethodSignatures(Type openGenericType)
    {
        HashSet<string> names = new HashSet<string>();
        foreach (System.Reflection.MethodInfo m in openGenericType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
        {
            names.Add(m.Name);
        }
        return names;
    }
}
