using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.Services.Services.AccountService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;
using Xunit;

namespace Sardanapal.Identity.Services.Tests.Unit;

public sealed class TestUser : IUser<long>
{
    public long Id { get; set; }
    public long CreateBy { get; set; }
    public long ModifiedBy { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime ModifiedOnUtc { get; set; }
    public bool IsDeleted { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string HashedPassword { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool VerifiedEmail { get; set; }
    public ulong? PhoneNumber { get; set; }
    public bool VerifiedPhoneNumber { get; set; }
    public bool MustChangePassword { get; set; }
}

public sealed class TestRole : IRoleBase<byte>
{
    public byte Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public sealed class TestUserRole : IUserRole<long, byte>
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public byte RoleId { get; set; }
}

internal sealed class TestAccountService
    : AccountServiceBase<
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM>,
        IRoleManager<long, byte, TestRole, TestUserRole>,
        long, TestUser, TestRole, TestUserRole,
        UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM>
{
    public TestAccountService(
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> userManager,
        IRoleManager<long, byte, TestRole, TestUserRole> roleManager,
        ILoginAttemptTracker attemptTracker)
        : base(userManager, roleManager, NullLogger.Instance, attemptTracker)
    {
    }
}

public class AccountServiceBaseTests
{
    private const string Username = "alice";
    private const string Password = "S3cret-pass";
    private const long UserId = 42L;

    private static IResponse<T> Ok<T>(T data) => new Response<T>(NullLogger.Instance)
    {
        StatusCode = StatusCode.Succeeded,
        Data = data
    };

    private static IResponse<T> Fail<T>(StatusCode status, string? message = null) => new Response<T>(NullLogger.Instance)
    {
        StatusCode = status,
        UserMessage = message ?? string.Empty
    };

    private static IResponse Fail(StatusCode status, string? message = null) => new Response(NullLogger.Instance)
    {
        StatusCode = status,
        UserMessage = message ?? string.Empty
    };

    private static ILoginAttemptTracker LockedTracker(int minutes)
    {
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        tracker.IsLockedOut(Arg.Any<string>()).Returns(true);
        tracker.GetLockoutRemaining(Arg.Any<string>()).Returns(TimeSpan.FromMinutes(minutes));
        return tracker;
    }

    private static ILoginAttemptTracker LockedTracker(TimeSpan remaining)
    {
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        tracker.IsLockedOut(Arg.Any<string>()).Returns(true);
        tracker.GetLockoutRemaining(Arg.Any<string>()).Returns(remaining);
        return tracker;
    }

    private static TestUser ExistingUser() => new TestUser
    {
        Id = UserId,
        Username = Username,
        HashedPassword = Utilities.HashPassword(Password)
    };

    private static IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> UserManager()
        => Substitute.For<IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM>>();

    private static IRoleManager<long, byte, TestRole, TestUserRole> RoleManager()
        => Substitute.For<IRoleManager<long, byte, TestRole, TestUserRole>>();

    private static TestAccountService CreateService(
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> userManager,
        ILoginAttemptTracker? tracker = null)
        => new TestAccountService(userManager, RoleManager(), tracker ?? Substitute.For<ILoginAttemptTracker>());

    [Fact]
    public async Task Login_Locked_Out_Returns_Failed_With_AccountLockedOut_Message()
    {
        TestAccountService service = CreateService(UserManager(), LockedTracker(15));
        LoginVM model = new LoginVM { Username = Username, Password = Password };

        IResponse<LoginDto> result = await service.Login(model);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Contain(Identity_Messages.AccountLockedOut.Split('{')[0].TrimEnd());
    }

    [Fact]
    public async Task Login_Locked_Out_Includes_Remaining_Minutes_In_Message()
    {
        const int remaining = 7;
        TestAccountService service = CreateService(UserManager(), LockedTracker(remaining));
        LoginVM model = new LoginVM { Username = Username, Password = Password };

        IResponse<LoginDto> result = await service.Login(model);

        result.UserMessage.Should().Contain(remaining.ToString());
        result.UserMessage.Should().Be(string.Format(Identity_Messages.AccountLockedOut, remaining));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(59)]
    public async Task Login_Lockout_Message_Ceilings_Subminute_Remainder_To_One(int seconds)
    {
        TestAccountService service = CreateService(UserManager(), LockedTracker(TimeSpan.FromSeconds(seconds)));
        LoginVM model = new LoginVM { Username = Username, Password = Password };

        IResponse<LoginDto> result = await service.Login(model);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(string.Format(Identity_Messages.AccountLockedOut, 1),
            "a sub-minute lockout remainder must be ceilinged to 1 minute, not truncated to 0");
        result.UserMessage.Should().NotContain("0 minute(s)",
            "truncation would wrongly report 0 minutes for a sub-minute remainder");
    }

    [Fact]
    public async Task Login_Valid_Credentials_Returns_Token_And_Records_Success()
    {
        TestUser user = ExistingUser();
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.GetUser(Username).Returns(Task.FromResult(Ok<TestUser>(user)));
        um.Login(UserId).Returns(Task.FromResult(Ok("jwt-token")));
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        TestAccountService service = CreateService(um, tracker);
        LoginVM model = new LoginVM { Username = Username, Password = Password };

        IResponse<LoginDto> result = await service.Login(model);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().NotBeNull();
        result.Data.Token.Should().Be("jwt-token");
        await um.Received(1).GetUser(Username);
        await um.Received(1).Login(UserId);
        tracker.Received(1).RecordSuccess(Username);
        tracker.DidNotReceive().RecordFailure(Arg.Any<string>());
    }

    [Fact]
    public async Task Login_Wrong_Password_Returns_WrongPassword_And_Records_Failure()
    {
        TestUser user = ExistingUser();
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.GetUser(Username).Returns(Task.FromResult(Ok<TestUser>(user)));
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        TestAccountService service = CreateService(um, tracker);
        LoginVM model = new LoginVM { Username = Username, Password = "totally-wrong" };

        IResponse<LoginDto> result = await service.Login(model);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(Identity_Messages.WrongPassword);
        tracker.Received(1).RecordFailure(Username);
        tracker.DidNotReceive().RecordSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task Login_Unknown_User_Uses_DummyHash_And_Returns_WrongPassword()
    {
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.GetUser(Username).Returns(Task.FromResult(Fail<TestUser>(StatusCode.NotExists)));
        TestAccountService service = CreateService(um, Substitute.For<ILoginAttemptTracker>());
        LoginVM model = new LoginVM { Username = Username, Password = Password };

        IResponse<LoginDto> result = await service.Login(model);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(Identity_Messages.WrongPassword);
        await um.DidNotReceive().Login(Arg.Any<long>());
    }

    [Fact]
    public async Task Login_Unknown_User_Records_Failure_For_AntiEnumeration()
    {
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.GetUser(Username).Returns(Task.FromResult(Fail<TestUser>(StatusCode.NotExists)));
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        TestAccountService service = CreateService(um, tracker);
        LoginVM model = new LoginVM { Username = Username, Password = Password };

        await service.Login(model);

        tracker.Received(1).RecordFailure(Username);
    }

    [Fact]
    public async Task Login_Null_AttemptTracker_Does_Not_Throw()
    {
        TestUser user = ExistingUser();
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.GetUser(Username).Returns(Task.FromResult(Ok<TestUser>(user)));
        um.Login(UserId).Returns(Task.FromResult(Ok("jwt")));
        TestAccountService service = new TestAccountService(um, RoleManager(), null!);
        LoginVM model = new LoginVM { Username = Username, Password = Password };

        Func<Task> act = async () => await service.Login(model);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Login_UserManager_Login_Fails_Propagates_Via_ConvertTo()
    {
        TestUser user = ExistingUser();
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.GetUser(Username).Returns(Task.FromResult(Ok<TestUser>(user)));
        um.Login(UserId).Returns(Task.FromResult(Fail<string>(StatusCode.Failed, "downstream failure")));
        TestAccountService service = CreateService(um);
        LoginVM model = new LoginVM { Username = Username, Password = Password };

        IResponse<LoginDto> result = await service.Login(model);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be("downstream failure");
    }

    [Fact]
    public async Task Register_Success_Returns_UserId()
    {
        RegisterVM<byte> registerModel = new RegisterVM<byte>
        {
            Username = "newuser",
            Password = "P4ssword",
            Roles = new List<byte> { 1 }
        };
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.RegisterUser(registerModel).Returns(Task.FromResult(Ok(99L)));
        TestAccountService service = CreateService(um);

        IResponse<long> result = await service.Register(registerModel);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().Be(99L);
    }

    [Fact]
    public async Task Register_Failure_Propagates_ConvertTo()
    {
        RegisterVM<byte> registerModel = new RegisterVM<byte>
        {
            Username = "dup",
            Password = "P4ssword",
            Roles = new List<byte> { 1 }
        };
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.RegisterUser(registerModel).Returns(Task.FromResult(Fail<long>(StatusCode.Duplicate, Identity_Messages.DuplicateUsername)));
        TestAccountService service = CreateService(um);

        IResponse<long> result = await service.Register(registerModel);

        result.StatusCode.Should().Be(StatusCode.Duplicate);
        result.UserMessage.Should().Be(Identity_Messages.DuplicateUsername);
    }

    [Fact]
    public async Task ChangePassword_ById_Delegates_To_UserManager()
    {
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.ChangePassword(UserId, "newpw").Returns(Task.FromResult(Fail(StatusCode.Succeeded)));
        TestAccountService service = CreateService(um);
        ChangePasswordVM<long> model = new ChangePasswordVM<long> { UserId = UserId, NewPassword = "newpw" };

        IResponse result = await service.ChangePassword(model);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        await um.Received(1).ChangePassword(UserId, "newpw");
    }

    [Fact]
    public async Task ChangePassword_ByUsername_Valid_Old_New_Succeeds()
    {
        TestUser user = ExistingUser();
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.GetUser(Username).Returns(Task.FromResult(Ok<TestUser>(user)));
        um.ChangePassword(UserId, "NewP4ssword").Returns(Task.FromResult(Fail(StatusCode.Succeeded)));
        TestAccountService service = CreateService(um);
        ChangePasswordVM model = new ChangePasswordVM
        {
            Username = Username,
            OldPassword = Password,
            NewPassword = "NewP4ssword"
        };

        IResponse result = await service.ChangePassword(model);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        await um.Received(1).ChangePassword(UserId, "NewP4ssword");
    }

    [Fact]
    public async Task ChangePassword_ByUsername_New_Equals_Old_Returns_DifferentPassword()
    {
        TestUser user = ExistingUser();
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.GetUser(Username).Returns(Task.FromResult(Ok<TestUser>(user)));
        TestAccountService service = CreateService(um);
        ChangePasswordVM model = new ChangePasswordVM
        {
            Username = Username,
            OldPassword = Password,
            NewPassword = Password
        };

        IResponse result = await service.ChangePassword(model);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(Identity_Messages.DifferentPassword);
        await um.DidNotReceive().ChangePassword(Arg.Any<long>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ChangePassword_ByUsername_Wrong_Old_Returns_WrongPassword()
    {
        TestUser user = ExistingUser();
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.GetUser(Username).Returns(Task.FromResult(Ok<TestUser>(user)));
        TestAccountService service = CreateService(um);
        ChangePasswordVM model = new ChangePasswordVM
        {
            Username = Username,
            OldPassword = "wrong-old",
            NewPassword = "NewP4ssword"
        };

        IResponse result = await service.ChangePassword(model);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(Identity_Messages.WrongPassword);
        await um.DidNotReceive().ChangePassword(Arg.Any<long>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ChangePassword_ByUsername_Unknown_User_AntiEnumeration_Returns_WrongPassword()
    {
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.GetUser(Username).Returns(Task.FromResult(Fail<TestUser>(StatusCode.NotExists)));
        TestAccountService service = CreateService(um);
        ChangePasswordVM model = new ChangePasswordVM
        {
            Username = Username,
            OldPassword = Password,
            NewPassword = "NewP4ssword"
        };

        IResponse result = await service.ChangePassword(model);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(Identity_Messages.WrongPassword);
        await um.DidNotReceive().ChangePassword(Arg.Any<long>(), Arg.Any<string>());
    }

    [Fact]
    public async Task RefreshToken_Success_Propagates_Token()
    {
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.RefreshToken(UserId).Returns(Task.FromResult(Ok("fresh-jwt")));
        TestAccountService service = CreateService(um);

        IResponse<string> result = await service.RefreshToken(UserId);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().Be("fresh-jwt");
    }

    [Fact]
    public async Task RefreshToken_Failure_Propagates_ConvertTo()
    {
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        um.RefreshToken(UserId).Returns(Task.FromResult(Fail<string>(StatusCode.NotExists, Identity_Messages.UserNotFound)));
        TestAccountService service = CreateService(um);

        IResponse<string> result = await service.RefreshToken(UserId);

        result.StatusCode.Should().Be(StatusCode.NotExists);
        result.UserMessage.Should().Be(Identity_Messages.UserNotFound);
    }

    [Fact]
    public async Task Edit_GetEditable_Delete_Delegate_To_UserManager()
    {
        IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM> um = UserManager();
        UserEditableVM editable = new UserEditableVM
        {
            Username = Username,
            Password = Password,
            Roles = new List<byte>(),
            Claims = new List<byte>()
        };
        um.GetEditable(UserId).Returns(Task.FromResult(Ok(editable)));
        um.Edit(UserId, editable).Returns(Task.FromResult(Fail(StatusCode.Succeeded)));
        um.DeleteUser(UserId).Returns(Task.FromResult(Fail(StatusCode.Succeeded)));
        TestAccountService service = CreateService(um);

        IResponse<UserEditableVM> getEditable = await service.GetEditable(UserId);
        IResponse edit = await service.Edit(UserId, editable);
        IResponse delete = await service.Delete(UserId);

        getEditable.StatusCode.Should().Be(StatusCode.Succeeded);
        edit.StatusCode.Should().Be(StatusCode.Succeeded);
        delete.StatusCode.Should().Be(StatusCode.Succeeded);
        await um.Received(1).GetEditable(UserId);
        await um.Received(1).Edit(UserId, editable);
        await um.Received(1).DeleteUser(UserId);
    }
}
