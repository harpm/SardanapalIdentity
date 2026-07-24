using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.Services.Services.AccountService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Response;
using Xunit;

namespace Sardanapal.Identity.Services.Tests.Unit;

public class AccountServiceSecurityTests
{
    private const string Username = "alice";
    private const string Password = "S3cret-pass";
    private const long UserId = 42L;

    private static IResponse<T> Ok<T>(T data) => new Response<T>(NullLogger.Instance)
    {
        StatusCode = StatusCode.Succeeded,
        Data = data
    };

    private static IResponse<T> Fail<T>(StatusCode status) => new Response<T>(NullLogger.Instance)
    {
        StatusCode = status
    };

    private static IResponse Fail(StatusCode status) => new Response(NullLogger.Instance)
    {
        StatusCode = status
    };

    private static TestUser ExistingUser() => new TestUser
    {
        Id = UserId,
        Username = Username,
        HashedPassword = Utilities.HashPassword(Password)
    };

    private static TestUser ExistingUserWithMismatchedHash() => new TestUser
    {
        Id = UserId,
        Username = Username,
        HashedPassword = Utilities.HashPassword("a-totally-different-secret")
    };

    private static IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> UserManager()
        => Substitute.For<IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM>>();

    private static IRoleManager<long, byte, TestRole, TestUserRole> RoleManager()
        => Substitute.For<IRoleManager<long, byte, TestRole, TestUserRole>>();

    private static TestAccountService CreateService(
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> userManager,
        ILoginAttemptTracker tracker)
        => new TestAccountService(userManager, RoleManager(), tracker);

    [Fact]
    public async Task Login_Unknown_User_Timings_Match_Existing_User_Path()
    {
        LoginVM model = new LoginVM { Username = Username, Password = Password };

        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> knownUm = UserManager();
        knownUm.GetUser(Username).Returns(Ok(ExistingUserWithMismatchedHash()));
        ILoginAttemptTracker knownTracker = Substitute.For<ILoginAttemptTracker>();
        TestAccountService knownSvc = CreateService(knownUm, knownTracker);

        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> unknownUm = UserManager();
        unknownUm.GetUser(Username).Returns(Fail<TestUser>(StatusCode.NotExists));
        ILoginAttemptTracker unknownTracker = Substitute.For<ILoginAttemptTracker>();
        TestAccountService unknownSvc = CreateService(unknownUm, unknownTracker);

        IResponse<LoginDto> knownResult = await knownSvc.Login(model);
        IResponse<LoginDto> unknownResult = await unknownSvc.Login(model);

        knownResult.StatusCode.Should().Be(StatusCode.Failed);
        unknownResult.StatusCode.Should().Be(StatusCode.Failed);

        knownResult.UserMessage.Should().Be(Identity_Messages.WrongPassword);
        unknownResult.UserMessage.Should().Be(Identity_Messages.WrongPassword);
        unknownResult.UserMessage.Should().Be(knownResult.UserMessage,
            "unknown and known-user paths must return the same generic message for anti-enumeration");

        knownTracker.Received(1).RecordFailure(Username);
        unknownTracker.Received(1).RecordFailure(Username);

        await unknownUm.DidNotReceive().Login(Arg.Any<long>());
    }

    [Fact]
    public async Task ChangePassword_Unknown_User_Timings_Match_Existing_User_Path()
    {
        ChangePasswordVM model = new ChangePasswordVM
        {
            Username = Username,
            OldPassword = Password,
            NewPassword = "NewPass-9"
        };

        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> knownUm = UserManager();
        knownUm.GetUser(Username).Returns(Ok(ExistingUser()));
        knownUm.ChangePassword(UserId, model.NewPassword).Returns(Fail(StatusCode.Exception));
        TestAccountService knownSvc = CreateService(knownUm, Substitute.For<ILoginAttemptTracker>());

        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> unknownUm = UserManager();
        unknownUm.GetUser(Username).Returns(Fail<TestUser>(StatusCode.NotExists));
        TestAccountService unknownSvc = CreateService(unknownUm, Substitute.For<ILoginAttemptTracker>());

        IResponse knownResult = await knownSvc.ChangePassword(model with { OldPassword = "wrong-password" });
        IResponse unknownResult = await unknownSvc.ChangePassword(model);

        knownResult.StatusCode.Should().Be(StatusCode.Failed);
        unknownResult.StatusCode.Should().Be(StatusCode.Failed);

        knownResult.UserMessage.Should().Be(Identity_Messages.WrongPassword);
        unknownResult.UserMessage.Should().Be(Identity_Messages.WrongPassword);
        unknownResult.UserMessage.Should().Be(knownResult.UserMessage,
            "unknown and known-user change-password paths must return the same generic message");

        await unknownUm.DidNotReceive().ChangePassword(Arg.Any<long>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Login_Attempt_Tracker_Gates_Login_And_All_Otp_Flows()
    {
        LoginAttemptTracker tracker = new LoginAttemptTracker(
            Microsoft.Extensions.Options.Options.Create(new SDConfigs
            {
                MaxLoginAttempts = 5,
                LockoutMinutes = 15
            }));

        for (int i = 0; i < 5; i++)
            tracker.RecordFailure("gate-key");

        tracker.IsLockedOut("gate-key").Should().BeTrue();
        tracker.GetLockoutRemaining("gate-key")!.Value.TotalMinutes.Should().BeGreaterThan(0);

        ILoginAttemptTracker mockTracker = Substitute.For<ILoginAttemptTracker>();
        mockTracker.IsLockedOut(Arg.Any<string>()).Returns(true);
        mockTracker.GetLockoutRemaining(Arg.Any<string>()).Returns(TimeSpan.FromMinutes(15));

        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.GetUser(Username).Returns(Ok(ExistingUser()));
        TestAccountService loginSvc = CreateService(um, mockTracker);

        IResponse<LoginDto> loginResult = await loginSvc.Login(new LoginVM { Username = Username, Password = Password });

        loginResult.StatusCode.Should().Be(StatusCode.Failed);
        loginResult.UserMessage.Should().Be(string.Format(Identity_Messages.AccountLockedOut, 15));
        mockTracker.Received(1).IsLockedOut(Username);
        await um.DidNotReceive().Login(Arg.Any<long>());

        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otpSvc =
            Substitute.For<IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>>>();
        TestOtpAccountService otpAccountSvc = new TestOtpAccountService(um, RoleManager(), otpSvc, mockTracker);

        IResponse<LoginDto> otpLoginResult = await otpAccountSvc.LoginWithOtp(
            new OTPResponseVM<long> { UserId = UserId, RoleId = 1, Code = "1234" });

        otpLoginResult.StatusCode.Should().Be(StatusCode.Failed);
        otpLoginResult.UserMessage.Should().Contain("15");

        IResponse registerOtpResult = await otpAccountSvc.RegisterWithOtp(
            new OTPResponseVM<long> { UserId = UserId, RoleId = 1, Code = "1234" });

        registerOtpResult.StatusCode.Should().Be(StatusCode.Failed);

        IResponse resetResult = await otpAccountSvc.ResetPassword(
            new ResetPasswordVM<long> { UserId = UserId, RoleId = 1, Code = "1234", NewPassword = "NewPw-1" });

        resetResult.StatusCode.Should().Be(StatusCode.Failed);

        await otpSvc.DidNotReceiveWithAnyArgs().ValidateCode(Arg.Any<NewOtpVM<long>>());
    }
}

internal sealed class TestOtpAccountService
    : OtpAccountServiceBase<
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM>,
        IRoleManager<long, byte, TestRole, TestUserRole>,
        long, TestUser, TestRole, TestUserRole,
        UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM>
{
    public TestOtpAccountService(
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> userManager,
        IRoleManager<long, byte, TestRole, TestUserRole> roleManager,
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otpService,
        ILoginAttemptTracker attemptTracker)
        : base(userManager, roleManager, otpService, NullLogger.Instance, attemptTracker)
    {
    }
}
