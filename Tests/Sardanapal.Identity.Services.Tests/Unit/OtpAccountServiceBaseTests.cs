using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.Services.Services.AccountService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.Identity.ViewModel.Models;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Models;
using Sardanapal.ViewModel.Response;
using Xunit;

namespace Sardanapal.Identity.Services.Tests.Unit;

public sealed class FakeUserManager : IUserManager<long, TestUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM>
{
    public TestUser? UserToReturn { get; set; }
    public StatusCode GetUserStatus { get; set; } = StatusCode.Succeeded;
    public string TokenToReturn { get; set; } = "otp-jwt";
    public StatusCode LoginStatus { get; set; } = StatusCode.Succeeded;
    public StatusCode ChangePasswordStatus { get; set; } = StatusCode.Succeeded;
    public StatusCode VerifyUserStatus { get; set; } = StatusCode.Succeeded;
    public List<object> GetUserCalls { get; } = new();
    public List<long> LoginCalls { get; } = new();
    public List<(long userId, string newPassword)> ChangePasswordCalls { get; } = new();
    public List<string> VerifyUserCalls { get; } = new();

    private IResponse<TestUser> MakeGetUserResponse()
    {
        Response<TestUser> r = new Response<TestUser>(NullLogger.Instance);
        if (GetUserStatus == StatusCode.Succeeded && UserToReturn != null)
        {
            r.StatusCode = StatusCode.Succeeded;
            r.Data = UserToReturn;
        }
        else
        {
            r.StatusCode = GetUserStatus;
        }
        return r;
    }

    public Task<IResponse<TestUser>> GetUser(long id)
    {
        GetUserCalls.Add(id);
        return Task.FromResult((IResponse<TestUser>)MakeGetUserResponse());
    }

    public Task<IResponse<TestUser>> GetUser(string username)
    {
        GetUserCalls.Add(username ?? string.Empty);
        return Task.FromResult((IResponse<TestUser>)MakeGetUserResponse());
    }

    public Task<IResponse<TestUser>> GetUser(ulong phone)
    {
        GetUserCalls.Add(phone);
        return Task.FromResult((IResponse<TestUser>)MakeGetUserResponse());
    }

    public Task<IResponse<string>> Login(long id)
    {
        LoginCalls.Add(id);
        Response<string> r = new Response<string>(NullLogger.Instance)
        {
            StatusCode = LoginStatus,
            Data = TokenToReturn
        };
        return Task.FromResult((IResponse<string>)r);
    }

    public Task<IResponse<string>> RefreshToken(long userId) => throw new NotSupportedException();
    public Task<IResponse<long>> RegisterUser(RegisterVM<byte> model) => throw new NotSupportedException();
    public Task<IResponse> Edit(long id, UserEditableVM model) => throw new NotSupportedException();

    public Task<IResponse> ChangePassword(long userId, string newPassword)
    {
        ChangePasswordCalls.Add((userId, newPassword));
        Response r = new Response(NullLogger.Instance) { StatusCode = ChangePasswordStatus };
        return Task.FromResult((IResponse)r);
    }

    public Task<IResponse> VerifyUser(string recipient)
    {
        VerifyUserCalls.Add(recipient ?? string.Empty);
        Response r = new Response(NullLogger.Instance) { StatusCode = VerifyUserStatus };
        return Task.FromResult((IResponse)r);
    }
    public Task<IResponse> DeleteUser(long userId) => throw new NotSupportedException();
    public Task<IResponse<UserVM<long>>> Get(long Id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<IResponse<GridVM<long, T>>> GetAll<T>(GridSearchModelVM<long, UserSearchVM> SearchModel = null, CancellationToken ct = default) where T : class => throw new NotSupportedException();
    public Task<IResponse<long>> Add(RegisterVM<byte> Model, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<IResponse<UserEditableVM>> GetEditable(long Id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<IResponse<bool>> Edit(long Id, UserEditableVM Model, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<IResponse<bool>> Delete(long Id, CancellationToken ct = default) => throw new NotSupportedException();
    public Task<IResponse<GridVM<long, SelectOptionVM<long, object>>>> GetDictionary(GridSearchModelVM<long, UserSearchVM> SearchModel = null, CancellationToken ct = default) => throw new NotSupportedException();
}

public sealed class TestableOtpAccountService
    : OtpAccountServiceBase<
        FakeUserManager,
        IRoleManager<long, byte, TestRole, TestUserRole>,
        long, TestUser, TestRole, TestUserRole,
        UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM>
{
    public TestableOtpAccountService(
        FakeUserManager userManager,
        IRoleManager<long, byte, TestRole, TestUserRole> roleManager,
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otpService,
        ILoginAttemptTracker attemptTracker)
        : base(userManager, roleManager, otpService, NullLogger.Instance, attemptTracker)
    {
    }

    public string OtpKeyPublic(long userId, byte roleId) => OtpKey(userId, roleId);
}

public class OtpAccountServiceBaseTests
{
    private const long UserId = 42L;
    private const byte RoleId = 3;
    private const string Username = "alice";
    private const string Email = "alice@example.com";
    private const ulong Phone = 9876543210UL;

    private static FakeUserManager UserManager() => new FakeUserManager();

    private static IRoleManager<long, byte, TestRole, TestUserRole> RoleManager()
        => Substitute.For<IRoleManager<long, byte, TestRole, TestUserRole>>();

    private static IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> OtpService()
        => Substitute.For<IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>>>();

    private static TestUser ExistingUser() => new TestUser
    {
        Id = UserId,
        Username = Username,
        Email = Email,
        PhoneNumber = Phone
    };

    private static TestUser UserWithVerifiedEmail() => new TestUser
    {
        Id = UserId,
        Username = Username,
        Email = Email,
        PhoneNumber = Phone,
        VerifiedEmail = true
    };

    private static TestUser UserWithVerifiedPhone() => new TestUser
    {
        Id = UserId,
        Username = Username,
        Email = Email,
        PhoneNumber = Phone,
        VerifiedPhoneNumber = true
    };

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

    private static TestableOtpAccountService CreateService(
        FakeUserManager um,
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>>? otp = null,
        ILoginAttemptTracker? tracker = null)
        => new TestableOtpAccountService(um, RoleManager(), otp ?? OtpService(), tracker ?? Substitute.For<ILoginAttemptTracker>());

    [Fact]
    public void OtpKey_Format_Is_Otp_Colon_UserId_Colon_RoleId()
    {
        TestableOtpAccountService service = CreateService(UserManager());

        string key = service.OtpKeyPublic(UserId, RoleId);

        key.Should().Be($"otp:{UserId}:{RoleId}");
    }

    [Fact]
    public async Task RequestLoginOtp_Phone_Resolves_User_By_Phone()
    {
        FakeUserManager um = UserManager();
        um.UserToReturn = ExistingUser();
        TestableOtpAccountService service = CreateService(um);
        OtpRequestVM model = new OtpRequestVM { PhoneNumber = Phone, Role = RoleId };

        await service.RequestLoginOtp(model);

        um.GetUserCalls.Should().ContainSingle().Which.Should().Be(Phone);
    }

    [Fact]
    public async Task RequestLoginOtp_Email_Resolves_User_By_Email()
    {
        FakeUserManager um = UserManager();
        um.UserToReturn = ExistingUser();
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        otp.Add(Arg.Any<NewOtpVM<long>>(), Arg.Any<CancellationToken>())
           .Returns(Task.FromResult((IResponse<Guid>)Ok(Guid.NewGuid())));
        TestableOtpAccountService service = CreateService(um, otp);
        OtpRequestVM model = new OtpRequestVM { Email = Email, Role = RoleId };

        await service.RequestLoginOtp(model);

        um.GetUserCalls.Should().ContainSingle().Which.Should().Be(Email);
        await otp.Received(1).Add(Arg.Is<NewOtpVM<long>>(m => (string)m.Recipient == Email), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestLoginOtp_No_Identifier_Returns_Canceled_With_InvalidEmailOrNumber()
    {
        TestableOtpAccountService service = CreateService(UserManager());
        OtpRequestVM model = new OtpRequestVM { Role = RoleId };

        IResponse<long> result = await service.RequestLoginOtp(model);

        result.StatusCode.Should().Be(StatusCode.Canceled);
        result.UserMessage.Should().Be(Identity_Messages.InvalidEmailOrNumber);
    }

    [Fact]
    public async Task RequestLoginOtp_User_Missing_Propagates_ConvertTo()
    {
        FakeUserManager um = UserManager();
        um.UserToReturn = null;
        um.GetUserStatus = StatusCode.NotExists;
        TestableOtpAccountService service = CreateService(um);
        OtpRequestVM model = new OtpRequestVM { Email = Email, Role = RoleId };

        IResponse<long> result = await service.RequestLoginOtp(model);

        result.StatusCode.Should().Be(StatusCode.NotExists);
    }

    [Fact]
    public async Task RequestLoginOtp_Success_Calls_OtpService_Add_With_Correct_VM()
    {
        FakeUserManager um = UserManager();
        um.UserToReturn = ExistingUser();
        NewOtpVM<long>? captured = null;
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        otp.Add(Arg.Do<NewOtpVM<long>>(vm => captured = vm), Arg.Any<CancellationToken>())
           .Returns(Task.FromResult((IResponse<Guid>)Ok(Guid.NewGuid())));
        TestableOtpAccountService service = CreateService(um, otp);
        OtpRequestVM model = new OtpRequestVM { Email = Email, Role = RoleId };

        await service.RequestLoginOtp(model);

        captured.Should().NotBeNull();
        captured!.Recipient.Should().Be(Email);
        captured.Username.Should().Be(Username);
        captured.UserId.Should().Be(UserId);
        captured.RoleId.Should().Be(RoleId);
    }

    [Fact]
    public async Task LoginWithOtp_Locked_Out_Returns_AccountLockedOut()
    {
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        tracker.IsLockedOut(Arg.Any<string>()).Returns(true);
        tracker.GetLockoutRemaining(Arg.Any<string>()).Returns(TimeSpan.FromMinutes(5));
        TestableOtpAccountService service = CreateService(UserManager(), tracker: tracker);
        OTPResponseVM<long> model = new OTPResponseVM<long> { UserId = UserId, RoleId = RoleId, Code = "1234" };

        IResponse<LoginDto> result = await service.LoginWithOtp(model);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(string.Format(Identity_Messages.AccountLockedOut, 5));
    }

    [Fact]
    public async Task LoginWithOtp_Validate_Fails_Records_Failure_And_Propagates()
    {
        FakeUserManager um = UserManager();
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        otp.ValidateCode(Arg.Any<NewOtpVM<long>>())
           .Returns(Task.FromResult(Fail<OtpVM<long>>(StatusCode.NotExists, Identity_Messages.InvalidOtpCode)));
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        TestableOtpAccountService service = CreateService(um, otp, tracker);
        OTPResponseVM<long> model = new OTPResponseVM<long> { UserId = UserId, RoleId = RoleId, Code = "0000" };

        IResponse<LoginDto> result = await service.LoginWithOtp(model);

        result.StatusCode.Should().Be(StatusCode.NotExists);
        result.UserMessage.Should().Be(Identity_Messages.InvalidOtpCode);
        tracker.Received(1).RecordFailure(Arg.Any<string>());
        tracker.DidNotReceive().RecordSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task LoginWithOtp_Validate_Ok_Records_Success_And_Logs_In()
    {
        FakeUserManager um = UserManager();
        um.UserToReturn = ExistingUser();
        um.TokenToReturn = "otp-jwt";
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        otp.ValidateCode(Arg.Any<NewOtpVM<long>>())
           .Returns(Task.FromResult((IResponse<OtpVM<long>>)Ok(new OtpVM<long>())));
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        TestableOtpAccountService service = CreateService(um, otp, tracker);
        OTPResponseVM<long> model = new OTPResponseVM<long> { UserId = UserId, RoleId = RoleId, Code = "1234" };

        IResponse<LoginDto> result = await service.LoginWithOtp(model);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().NotBeNull();
        result.Data.Token.Should().Be("otp-jwt");
        tracker.Received(1).RecordSuccess(Arg.Any<string>());
        tracker.DidNotReceive().RecordFailure(Arg.Any<string>());
        um.LoginCalls.Should().ContainSingle().Which.Should().Be(UserId);
    }

    [Fact]
    public async Task LoginWithOtp_Null_AttemptTracker_Does_Not_Throw()
    {
        FakeUserManager um = UserManager();
        um.UserToReturn = ExistingUser();
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        otp.ValidateCode(Arg.Any<NewOtpVM<long>>())
           .Returns(Task.FromResult((IResponse<OtpVM<long>>)Ok(new OtpVM<long>())));
        TestableOtpAccountService service = new TestableOtpAccountService(um, RoleManager(), otp, null!);
        OTPResponseVM<long> model = new OTPResponseVM<long> { UserId = UserId, RoleId = RoleId, Code = "1234" };

        Func<Task> act = async () => await service.LoginWithOtp(model);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RequestRegisterOtp_Already_Verified_Email_Returns_Duplicate_AlreadyVerified()
    {
        FakeUserManager um = UserManager();
        um.UserToReturn = UserWithVerifiedEmail();
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        TestableOtpAccountService service = CreateService(um, otp);
        OtpRequestVM model = new OtpRequestVM { Email = Email, Role = RoleId };

        IResponse<long> result = await service.RequestRegisterOtp(model);

        result.StatusCode.Should().Be(StatusCode.Duplicate);
        result.UserMessage.Should().Be(Identity_Messages.AlreadyVerified);
        await otp.DidNotReceive().Add(Arg.Any<NewOtpVM<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestRegisterOtp_Already_Verified_Phone_Returns_Duplicate_AlreadyVerified()
    {
        FakeUserManager um = UserManager();
        um.UserToReturn = UserWithVerifiedPhone();
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        TestableOtpAccountService service = CreateService(um, otp);
        OtpRequestVM model = new OtpRequestVM { PhoneNumber = Phone, Role = RoleId };

        IResponse<long> result = await service.RequestRegisterOtp(model);

        result.StatusCode.Should().Be(StatusCode.Duplicate);
        result.UserMessage.Should().Be(Identity_Messages.AlreadyVerified);
        await otp.DidNotReceive().Add(Arg.Any<NewOtpVM<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestRegisterOtp_Not_Verified_Calls_Add()
    {
        FakeUserManager um = UserManager();
        um.UserToReturn = ExistingUser();
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        otp.Add(Arg.Any<NewOtpVM<long>>(), Arg.Any<CancellationToken>())
           .Returns(Task.FromResult((IResponse<Guid>)Ok(Guid.NewGuid())));
        TestableOtpAccountService service = CreateService(um, otp);
        OtpRequestVM model = new OtpRequestVM { Email = Email, Role = RoleId };

        await service.RequestRegisterOtp(model);

        await otp.Received(1).Add(Arg.Any<NewOtpVM<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterWithOtp_Lockout_Path()
    {
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        tracker.IsLockedOut(Arg.Any<string>()).Returns(true);
        tracker.GetLockoutRemaining(Arg.Any<string>()).Returns(TimeSpan.FromMinutes(9));
        TestableOtpAccountService service = CreateService(UserManager(), tracker: tracker);
        OTPResponseVM<long> model = new OTPResponseVM<long> { UserId = UserId, RoleId = RoleId, Code = "1234" };

        IResponse result = await service.RegisterWithOtp(model);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(string.Format(Identity_Messages.AccountLockedOut, 9));
    }

    [Fact]
    public async Task RegisterWithOtp_Validate_Fail_Propagates()
    {
        FakeUserManager um = UserManager();
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        otp.ValidateCode(Arg.Any<NewOtpVM<long>>())
           .Returns(Task.FromResult(Fail<OtpVM<long>>(StatusCode.Failed, Identity_Messages.InvalidOtpCode)));
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        TestableOtpAccountService service = CreateService(um, otp, tracker);
        OTPResponseVM<long> model = new OTPResponseVM<long> { UserId = UserId, RoleId = RoleId, Code = "0000" };

        IResponse result = await service.RegisterWithOtp(model);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(Identity_Messages.InvalidOtpCode);
        tracker.Received(1).RecordFailure(Arg.Any<string>());
        tracker.DidNotReceive().RecordSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task RegisterWithOtp_Success_Calls_VerifyUser()
    {
        const string recipient = Email;
        FakeUserManager um = UserManager();
        um.UserToReturn = ExistingUser();
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        otp.ValidateCode(Arg.Any<NewOtpVM<long>>())
           .Returns(Task.FromResult((IResponse<OtpVM<long>>)Ok(new OtpVM<long> { Recipient = recipient })));
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        TestableOtpAccountService service = CreateService(um, otp, tracker);
        OTPResponseVM<long> model = new OTPResponseVM<long> { UserId = UserId, RoleId = RoleId, Code = "1234" };

        IResponse result = await service.RegisterWithOtp(model);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        tracker.Received(1).RecordSuccess(Arg.Any<string>());
        um.VerifyUserCalls.Should().ContainSingle().Which.Should().Be(recipient);
    }

    [Fact]
    public async Task RegisterWithOtp_User_Missing_Propagates_ConvertTo()
    {
        FakeUserManager um = UserManager();
        um.UserToReturn = null;
        um.GetUserStatus = StatusCode.NotExists;
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        otp.ValidateCode(Arg.Any<NewOtpVM<long>>())
           .Returns(Task.FromResult((IResponse<OtpVM<long>>)Ok(new OtpVM<long>())));
        TestableOtpAccountService service = CreateService(um, otp);
        OTPResponseVM<long> model = new OTPResponseVM<long> { UserId = UserId, RoleId = RoleId, Code = "1234" };

        IResponse result = await service.RegisterWithOtp(model);

        result.StatusCode.Should().Be(StatusCode.NotExists);
        um.VerifyUserCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task RequestResetPassword_Resolves_Identifier_And_Calls_Add()
    {
        FakeUserManager um = UserManager();
        um.UserToReturn = ExistingUser();
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        otp.Add(Arg.Any<NewOtpVM<long>>(), Arg.Any<CancellationToken>())
           .Returns(Task.FromResult((IResponse<Guid>)Ok(Guid.NewGuid())));
        TestableOtpAccountService service = CreateService(um, otp);
        ResetPasswordRequestVM model = new ResetPasswordRequestVM { Email = Email };

        IResponse<long> result = await service.RequestResetPassword(model);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().Be(UserId);
        um.GetUserCalls.Should().Contain(Email);
        await otp.Received(1).Add(Arg.Any<NewOtpVM<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestResetPassword_No_Identifier_Returns_Canceled()
    {
        TestableOtpAccountService service = CreateService(UserManager());
        ResetPasswordRequestVM model = new ResetPasswordRequestVM();

        IResponse<long> result = await service.RequestResetPassword(model);

        result.StatusCode.Should().Be(StatusCode.Canceled);
        result.UserMessage.Should().Be(Identity_Messages.InvalidEmailOrNumber);
    }

    [Fact]
    public async Task ResetPassword_Lockout_Path()
    {
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        tracker.IsLockedOut(Arg.Any<string>()).Returns(true);
        tracker.GetLockoutRemaining(Arg.Any<string>()).Returns(TimeSpan.FromMinutes(11));
        TestableOtpAccountService service = CreateService(UserManager(), tracker: tracker);
        ResetPasswordVM<long> model = new ResetPasswordVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "1234",
            NewPassword = "NewP4ss"
        };

        IResponse result = await service.ResetPassword(model);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(string.Format(Identity_Messages.AccountLockedOut, 11));
    }

    [Fact]
    public async Task ResetPassword_Validate_Fail_Propagates()
    {
        FakeUserManager um = UserManager();
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        otp.ValidateCode(Arg.Any<NewOtpVM<long>>())
           .Returns(Task.FromResult(Fail<OtpVM<long>>(StatusCode.NotExists, Identity_Messages.InvalidOtpCode)));
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        TestableOtpAccountService service = CreateService(um, otp, tracker);
        ResetPasswordVM<long> model = new ResetPasswordVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "0000",
            NewPassword = "NewP4ss"
        };

        IResponse result = await service.ResetPassword(model);

        result.StatusCode.Should().Be(StatusCode.NotExists);
        result.UserMessage.Should().Be(Identity_Messages.InvalidOtpCode);
        tracker.Received(1).RecordFailure(Arg.Any<string>());
        tracker.DidNotReceive().RecordSuccess(Arg.Any<string>());
    }

    [Fact]
    public async Task ResetPassword_Success_Calls_ChangePassword()
    {
        const string newPassword = "NewP4ss";
        FakeUserManager um = UserManager();
        um.UserToReturn = ExistingUser();
        IOtpService<long, Guid, OtpVM<long>, NewOtpVM<long>, OtpEditableVM<long>> otp = OtpService();
        otp.ValidateCode(Arg.Any<NewOtpVM<long>>())
           .Returns(Task.FromResult((IResponse<OtpVM<long>>)Ok(new OtpVM<long>())));
        ILoginAttemptTracker tracker = Substitute.For<ILoginAttemptTracker>();
        TestableOtpAccountService service = CreateService(um, otp, tracker);
        ResetPasswordVM<long> model = new ResetPasswordVM<long>
        {
            UserId = UserId,
            RoleId = RoleId,
            Code = "1234",
            NewPassword = newPassword
        };

        IResponse result = await service.ResetPassword(model);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        tracker.Received(1).RecordSuccess(Arg.Any<string>());
        um.ChangePasswordCalls.Should().ContainSingle().Which.Should().Be((UserId, newPassword));
    }

    [Fact]
    public async Task All_Request_Methods_Report_InvalidEmailOrNumber_On_UserMessage_When_No_Identifier()
    {
        TestableOtpAccountService service = CreateService(UserManager());
        OtpRequestVM otpModel = new OtpRequestVM { Role = RoleId };
        ResetPasswordRequestVM resetModel = new ResetPasswordRequestVM();

        IResponse<long> login = await service.RequestLoginOtp(otpModel);
        IResponse<long> register = await service.RequestRegisterOtp(otpModel);
        IResponse<long> reset = await service.RequestResetPassword(resetModel);

        login.StatusCode.Should().Be(StatusCode.Canceled);
        register.StatusCode.Should().Be(StatusCode.Canceled);
        reset.StatusCode.Should().Be(StatusCode.Canceled);

        login.UserMessage.Should().Be(Identity_Messages.InvalidEmailOrNumber);
        register.UserMessage.Should().Be(Identity_Messages.InvalidEmailOrNumber);
        reset.UserMessage.Should().Be(Identity_Messages.InvalidEmailOrNumber);

        login.UserMessage.Should().Be(register.UserMessage,
            "RequestLoginOtp and RequestRegisterOtp must report the no-identifier message on the same property as RequestResetPassword");
        reset.UserMessage.Should().Be(register.UserMessage);
    }
}
