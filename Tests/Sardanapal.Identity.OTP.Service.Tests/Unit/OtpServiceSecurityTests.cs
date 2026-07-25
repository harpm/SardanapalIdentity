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
using Sardanapal.ViewModel.Response;
using Xunit;

namespace Sardanapal.Identity.OTP.Service.Tests.Unit;

public class OtpServiceSecurityTests
{
    private const long UserId = 21L;
    private const byte RoleId = 3;
    private const string Recipient = "otp@example.com";

    private static IMapper CreateMapper()
    {
        MapperConfiguration config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NewOtpVM<long>, TestOtpModel>();
            cfg.CreateMap<TestOtpModel, OtpVM<long>>();
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

        public NewOtpVM<long> NewModel() => new NewOtpVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Recipient = Recipient,
            Username = "bob"
        };
    }

    private static TestOtpModel Otp(string code, DateTime expire, Guid? id = null) => new TestOtpModel
    {
        Id = id ?? Guid.NewGuid(),
        Code = code,
        UserId = UserId,
        RoleId = RoleId,
        ExpireTime = expire
    };

    private static TestOtpModel OtpWithRole(long userId, byte roleId, string code, DateTime expire, Guid? id = null)
        => new TestOtpModel
        {
            Id = id ?? Guid.NewGuid(),
            Code = code,
            UserId = userId,
            RoleId = roleId,
            ExpireTime = expire
        };

    private static void SetupRepo(ITestOtpRepository repo, params TestOtpModel[] items)
    {
        List<TestOtpModel> list = new List<TestOtpModel>(items);
        repo.FetchAll().Returns(list);
        repo.FetchAllAsync(Arg.Any<CancellationToken>()).Returns(list);
    }

    [Fact]
    public async Task Otp_ValidateCode_Rejects_Expired_And_Deletes_After_Success_No_Replay()
    {
        Deps d = new Deps();
        Guid otpId = Guid.NewGuid();
        TestOtpModel active = Otp("4242", DateTime.UtcNow.AddMinutes(3), otpId);
        List<TestOtpModel> store = new List<TestOtpModel> { active };
        d.Repo.FetchAll().Returns(store);
        d.Repo.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(c => store.RemoveAll(x => x.Id == (Guid)c[0]));

        var svc = d.BuildService(5);

        TestOtpModel expired = Otp("1111", DateTime.UtcNow.AddMinutes(-1));
        List<TestOtpModel> expiredStore = new List<TestOtpModel> { expired };
        Deps expiredDeps = new Deps();
        expiredDeps.Repo.FetchAll().Returns(expiredStore);
        var expiredSvc = expiredDeps.BuildService(5);

        IResponse<OtpVM<long>> expiredResult = await expiredSvc.ValidateCode(new NewOtpVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "1111"
        });

        expiredResult.StatusCode.Should().Be(StatusCode.Failed);
        expiredResult.UserMessage.Should().Be(Identity_Messages.OtpCodeExpired);
        await expiredDeps.Repo.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        IResponse<OtpVM<long>> firstValidate = await svc.ValidateCode(new NewOtpVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "4242"
        });

        firstValidate.StatusCode.Should().Be(StatusCode.Succeeded);
        await d.Repo.Received(1).DeleteAsync(otpId, Arg.Any<CancellationToken>());

        store.Should().BeEmpty("the OTP must be deleted after a successful match");

        IResponse<OtpVM<long>> secondValidate = await svc.ValidateCode(new NewOtpVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "4242"
        });

        secondValidate.StatusCode.Should().Be(StatusCode.NotExists,
            "a successfully consumed OTP must not be replayable");
    }

    [Fact]
    public async Task Otp_Add_Enforces_Cooldown_Per_User_Role()
    {
        Deps d = new Deps();
        SetupRepo(d.Repo, Otp("9999", DateTime.UtcNow.AddMinutes(2)));
        d.Helper.GenerateNewOtp().Returns("0000");
        var svc = d.BuildService(6);

        IResponse<Guid> result = await svc.Add(d.NewModel());

        result.StatusCode.Should().Be(StatusCode.Canceled);
        result.UserMessage.Should().Be(string.Format(Identity_Messages.OtpCooldown, 6));
        await d.Repo.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<TestOtpModel>(), Arg.Any<CancellationToken>());
        d.Email.DidNotReceiveWithAnyArgs().Send(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        Deps fresh = new Deps();
        SetupRepo(fresh.Repo);
        fresh.Helper.GenerateNewOtp().Returns("1234");
        var freshSvc = fresh.BuildService(6);

        IResponse<Guid> differentRoleResult = await freshSvc.Add(new NewOtpVM<long>
        {
            UserId = UserId,
            RoleId = (byte)(RoleId + 1),
            Recipient = Recipient,
            Username = "bob"
        });

        differentRoleResult.StatusCode.Should().Be(StatusCode.Succeeded);
        await fresh.Repo.Received(1).AddAsync(Arg.Any<TestOtpModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_Cooldown_Isolates_Per_User_Role_In_Same_Store()
    {
        const byte otherRoleId = (byte)(RoleId + 1);

        List<TestOtpModel> store = new List<TestOtpModel>
        {
            OtpWithRole(UserId, RoleId, "9999", DateTime.UtcNow.AddMinutes(2))
        };

        Deps d = new Deps();
        d.Repo.FetchAll().Returns(store);
        d.Repo.FetchAllAsync(Arg.Any<CancellationToken>()).Returns(store);
        d.Repo.AddAsync(Arg.Do<TestOtpModel>(m => m.Id = Guid.NewGuid()), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(c => store.Add((TestOtpModel)c[0]));
        d.Helper.GenerateNewOtp().Returns("4242");
        var svc = d.BuildService(6);

        IResponse<Guid> sameRoleResult = await svc.Add(d.NewModel());

        sameRoleResult.StatusCode.Should().Be(StatusCode.Canceled,
            "an active OTP for the same user and role must trigger cooldown even when other roles exist");
        sameRoleResult.UserMessage.Should().Be(string.Format(Identity_Messages.OtpCooldown, 6));
        await d.Repo.DidNotReceiveWithAnyArgs().AddAsync(Arg.Any<TestOtpModel>(), Arg.Any<CancellationToken>());

        IResponse<Guid> differentRoleResult = await svc.Add(new NewOtpVM<long>
        {
            UserId = UserId,
            RoleId = otherRoleId,
            Recipient = Recipient,
            Username = "bob"
        });

        differentRoleResult.StatusCode.Should().Be(StatusCode.Succeeded,
            "a different role on the same user must NOT be blocked by an OTP held under another role in the same store");
        await d.Repo.Received(1).AddAsync(
            Arg.Is<TestOtpModel>(m => m.UserId == UserId && m.RoleId == otherRoleId),
            Arg.Any<CancellationToken>());
        store.Should().HaveCount(2,
            "the new OTP for the other role must have been persisted alongside the original one");
    }
}
