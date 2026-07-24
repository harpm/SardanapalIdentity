using System.Reflection;
using System.Text.Json;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sardanapal.Contract.IModel;
using Sardanapal.Contract.IService;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.OTP.Domain;
using Sardanapal.Identity.OTP.Services;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.RedisCache.Services;
using Sardanapal.ViewModel.Response;
using StackExchange.Redis;
using Xunit;

namespace Sardanapal.Identity.OTP.Service.Tests.Unit;

public sealed class TestOtpCacheModel : OTPModel<long, Guid>
{
}

internal sealed class TestableOtpCacheService
    : OtpCacheService<long, Guid, TestOtpCacheModel, CacheNewOtpVM<long, Guid>, CacheOtpEditableVM<long, Guid>>
{
    private IEnumerable<TestOtpCacheModel> _allItems = Enumerable.Empty<TestOtpCacheModel>();

    public TestableOtpCacheService(
        IConnectionMultiplexer conn,
        IMapper mapper,
        IOtpHelper helper,
        IEmailService email,
        ISmsService sms,
        ILogger logger)
        : base(conn, mapper, helper, email, sms, logger)
    {
    }

    public void SetExpireMinutes(int minutes) => expireTime = minutes;

    public void SetInternalItems(IEnumerable<TestOtpCacheModel> items) => _allItems = items;

    protected override Task<IEnumerable<TestOtpCacheModel>> InternalGetAll()
    {
        return Task.FromResult(_allItems);
    }
}

public class OtpCacheServiceTests
{
    private const long UserId = 11L;
    private const byte RoleId = 2;
    private const string EmailRecipient = "alice@example.com";
    private const string SmsRecipient = "9876543210";

    private static IMapper CreateMapper()
    {
        MapperConfiguration config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CacheNewOtpVM<long, Guid>, TestOtpCacheModel>();
        });
        return config.CreateMapper();
    }

    private sealed class Deps
    {
        public IConnectionMultiplexer Conn { get; } = Substitute.For<IConnectionMultiplexer>();
        public IDatabase Database { get; } = Substitute.For<IDatabase>();
        public IMapper Mapper { get; } = CreateMapper();
        public IOtpHelper Helper { get; } = Substitute.For<IOtpHelper>();
        public IEmailService Email { get; } = Substitute.For<IEmailService>();
        public ISmsService Sms { get; } = Substitute.For<ISmsService>();

        public TestableOtpCacheService BuildService(int expireMinutes = 5)
        {
            Conn.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(Database);
            Database.HashGetAllAsync(Arg.Any<RedisKey>(), CommandFlags.None).Returns(Array.Empty<HashEntry>());
            Database.HashSetAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<RedisValue>(), When.Always, CommandFlags.None)
                .Returns(Task.FromResult(false));
            Database.ExecuteAsync(Arg.Any<string>(), Arg.Any<object[]>()).Returns(RedisResult.Create(RedisValue.Null, ResultType.None));

            TestableOtpCacheService svc = new TestableOtpCacheService(Conn, Mapper, Helper, Email, Sms, NullLogger.Instance);
            svc.SetExpireMinutes(expireMinutes);
            return svc;
        }

        public CacheNewOtpVM<long, Guid> NewModel(string recipient) => new CacheNewOtpVM<long, Guid>
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            RoleId = RoleId,
            Recipient = recipient,
            Username = "alice"
        };

        public TestOtpCacheModel Existing(string code, DateTime expire) => new TestOtpCacheModel
        {
            Id = Guid.NewGuid(),
            Code = code,
            UserId = UserId,
            RoleId = RoleId,
            ExpireTime = expire
        };
    }

    [Fact]
    public async Task Add_Cooldown_Per_User_Role()
    {
        Deps d = new Deps();
        TestableOtpCacheService svc = d.BuildService(5);
        svc.SetInternalItems(new[] { d.Existing("1111", DateTime.UtcNow.AddMinutes(2)) });
        d.Helper.GenerateNewOtp().Returns("2222");

        IResponse<Guid> result = await svc.Add(d.NewModel(EmailRecipient));

        result.StatusCode.Should().Be(StatusCode.Canceled);
        result.UserMessage.Should().Be(string.Format(Identity_Messages.OtpCooldown, 5));
        await d.Database.DidNotReceiveWithAnyArgs().HashSetAsync(
            Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<RedisValue>(), Arg.Any<When>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task Add_Sets_TTL_Via_HEXPIRE()
    {
        Deps d = new Deps();
        const int minutes = 7;
        TestableOtpCacheService svc = d.BuildService(minutes);
        d.Helper.GenerateNewOtp().Returns("3333");

        await svc.Add(d.NewModel(EmailRecipient));

        await d.Database.Received(1).ExecuteAsync(
            "HEXPIRE",
            Arg.Is<object[]>(args => args != null && args.Any(a => a is long && (long)a == minutes * 60)));
    }

    [Fact]
    public async Task Add_Routes_Sms_Or_Email()
    {
        Deps smsDeps = new Deps();
        TestableOtpCacheService smsSvc = smsDeps.BuildService(5);
        smsDeps.Helper.GenerateNewOtp().Returns("1234");

        await smsSvc.Add(smsDeps.NewModel(SmsRecipient));

        smsDeps.Sms.Received(1).Send(SmsRecipient, "1234", Arg.Any<CancellationToken>());
        smsDeps.Email.DidNotReceiveWithAnyArgs().Send(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        Deps emailDeps = new Deps();
        TestableOtpCacheService emailSvc = emailDeps.BuildService(5);
        emailDeps.Helper.GenerateNewOtp().Returns("4321");

        await emailSvc.Add(emailDeps.NewModel(EmailRecipient));

        emailDeps.Email.Received(1).Send(EmailRecipient, "4321", Arg.Any<CancellationToken>());
        emailDeps.Sms.DidNotReceiveWithAnyArgs().Send(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateOtpRegister_True_On_Match()
    {
        Deps d = new Deps();
        TestableOtpCacheService svc = d.BuildService(5);
        svc.SetInternalItems(new[] { d.Existing("9999", DateTime.UtcNow.AddMinutes(2)) });

        IResponse<bool> result = await svc.ValidateOtpRegister(new OTPResponseVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "9999"
        });

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateOtpRegister_False_On_Mismatch()
    {
        Deps d = new Deps();
        TestableOtpCacheService svc = d.BuildService(5);
        svc.SetInternalItems(new[] { d.Existing("9999", DateTime.UtcNow.AddMinutes(2)) });

        IResponse<bool> result = await svc.ValidateOtpRegister(new OTPResponseVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "0000"
        });

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateOtpLogin_True_On_Match()
    {
        Deps d = new Deps();
        TestableOtpCacheService svc = d.BuildService(5);
        svc.SetInternalItems(new[] { d.Existing("5555", DateTime.UtcNow.AddMinutes(2)) });

        IResponse<bool> result = await svc.ValidateOtpLogin(new OTPResponseVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "5555"
        });

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateOtpRegister_NotExists_When_No_Items()
    {
        Deps d = new Deps();
        TestableOtpCacheService svc = d.BuildService(5);
        svc.SetInternalItems(null!);

        IResponse<bool> result = await svc.ValidateOtpRegister(new OTPResponseVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "1234"
        });

        result.StatusCode.Should().Be(StatusCode.NotExists);
    }
}
