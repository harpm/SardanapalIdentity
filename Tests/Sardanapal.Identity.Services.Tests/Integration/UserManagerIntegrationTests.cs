using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.Repository;
using Sardanapal.Identity.Services.Services;
using Sardanapal.Identity.Services.Services.UserManager;
using Sardanapal.Identity.Share.Static;
using Sardanapal.Identity.Share.Types;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;
using Xunit;

namespace Sardanapal.Identity.Services.Tests.Integration;

public sealed class IntUser : IUser<long>
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

public sealed class IntUserRole : IUserRole<long, byte>
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public byte RoleId { get; set; }
}

public sealed class IntUserClaim : IUserClaim<long, byte>
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public byte ClaimId { get; set; }
}

public sealed class IntClaim : IClaim<byte>, IControllerActionClaim<byte>
{
    public byte Id { get; set; }
    public byte ClaimType => (byte)SdClaimType.ControllerAction;
    public Guid ControllerId { get; set; }
    public byte ActionType { get; set; }
}

public class IntUserRepository : UserRepositoryBase<long, byte, IntUser, IntUserRole, IntUserClaim, IntClaim>
{
    private long _userIdCounter;

    public override void Add(IntUser model, CancellationToken ct = default)
    {
        if (model.Id == 0)
        {
            model.Id = Interlocked.Increment(ref _userIdCounter);
        }
        base.Add(model, ct);
    }

    public override Task AddAsync(IntUser model, CancellationToken ct = default)
    {
        if (model.Id == 0)
        {
            model.Id = Interlocked.Increment(ref _userIdCounter);
        }
        return base.AddAsync(model, ct);
    }

    public IEnumerable<IntUser> Storage => _db.Values;
}

internal sealed class FailingUserRepository : IntUserRepository
{
    public override Task<long> AddUserRoleAsync(IntUserRole userRole)
    {
        throw new InvalidOperationException("forced failure during role assignment");
    }
}

internal sealed class TestableUserManager
    : UserManager<IntUserRepository, long, IntUser, UserSearchVM, UserVM<long>, RegisterVM<byte>, UserEditableVM,
        IntUserRole, IntUserClaim, IntClaim>
{
    public TestableUserManager(IntUserRepository repository, IMapper mapper, ITokenService tokenService)
        : base(repository, mapper, NullLogger.Instance, tokenService)
    {
    }

    public IEnumerable<IntUser> SearchPublic(IEnumerable<IntUser> entities, UserSearchVM searchVM)
        => Search(entities, searchVM);
}

public class UserManagerIntegrationTests
{
    private const string Username = "alice";
    private const string Password = "S3cret-pass";
    private const string Email = "alice@example.com";
    private const ulong Phone = 9876543210UL;
    private const byte RoleId = 1;

    private static SymmetricSecurityKey SigningKey =>
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes("integration-test-secret-key-at-least-32-bytes-long"));

    private static TokenValidationParameters ValidationParameters() => new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = SigningKey,
        ClockSkew = TimeSpan.Zero
    };

    private static SDConfigs Configs(int expiration = 30) => new SDConfigs
    {
        ExpirationTime = expiration,
        TokenParameters = ValidationParameters()
    };

    private static ITokenService TokenService(int expiration = 30)
        => new TokenService(Options.Create(Configs(expiration)), NullLogger.Instance);

    private static IMapper CreateMapper()
    {
        MapperConfiguration config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<UserEditableVM, IntUser>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.HashedPassword, opt => opt.Ignore())
                .ForMember(d => d.Email, opt => opt.Ignore())
                .ForMember(d => d.VerifiedEmail, opt => opt.Ignore())
                .ForMember(d => d.PhoneNumber, opt => opt.Ignore())
                .ForMember(d => d.VerifiedPhoneNumber, opt => opt.Ignore())
                .ForMember(d => d.MustChangePassword, opt => opt.Ignore())
                .ForMember(d => d.FirstName, opt => opt.Ignore())
                .ForMember(d => d.LastName, opt => opt.Ignore())
                .ForMember(d => d.CreateBy, opt => opt.Ignore())
                .ForMember(d => d.ModifiedBy, opt => opt.Ignore())
                .ForMember(d => d.CreatedOnUtc, opt => opt.Ignore())
                .ForMember(d => d.ModifiedOnUtc, opt => opt.Ignore())
                .ForMember(d => d.IsDeleted, opt => opt.Ignore());
        });
        return config.CreateMapper();
    }

    private static IntUserRepository NewRepo() => new IntUserRepository();

    private static TestableUserManager NewService(IntUserRepository? repo = null, int expiration = 30)
        => new TestableUserManager(repo ?? NewRepo(), CreateMapper(), TokenService(expiration));

    private static IntUser SeedUser(IntUserRepository repo, string username = Username, string? email = Email,
        ulong? phone = Phone, bool mustChangePassword = false)
    {
        IntUser user = new IntUser
        {
            Username = username,
            HashedPassword = Utilities.HashPassword(Password),
            Email = email,
            PhoneNumber = phone,
            MustChangePassword = mustChangePassword
        };
        repo.Add(user);
        return user;
    }

    private static JwtSecurityToken ReadJwt(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    [Fact]
    public async Task Search_Filters_By_Username_Email_Phone()
    {
        IntUserRepository repo = NewRepo();
        SeedUser(repo, username: "alice", email: "alice@example.com", phone: 9876543210UL);
        SeedUser(repo, username: "bob", email: "bob@example.com", phone: 9123456789UL);
        SeedUser(repo, username: "carol", email: "carol@example.com", phone: 9555444333UL);
        TestableUserManager svc = NewService(repo);
        List<IntUser> all = repo.Storage.ToList();

        IEnumerable<IntUser> byUsername = svc.SearchPublic(all, new UserSearchVM { Username = "ali" });
        IEnumerable<IntUser> byEmail = svc.SearchPublic(all, new UserSearchVM { Email = "bob@" });
        IEnumerable<IntUser> byPhone = svc.SearchPublic(all, new UserSearchVM { PhoneNumber = 9555444333L });

        byUsername.Should().ContainSingle(u => u.Username == "alice");
        byEmail.Should().ContainSingle(u => u.Username == "bob");
        byPhone.Should().ContainSingle(u => u.Username == "carol");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Search_Null_SearchVM_Returns_All()
    {
        IntUserRepository repo = NewRepo();
        SeedUser(repo, username: "alice");
        SeedUser(repo, username: "bob");
        TestableUserManager svc = NewService(repo);
        List<IntUser> all = repo.Storage.ToList();

        IEnumerable<IntUser> result = svc.SearchPublic(all, null!);

        result.Should().HaveCount(2);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetUser_By_String_Matches_Email_Or_Username_Or_Phone()
    {
        IntUserRepository repo = NewRepo();
        IntUser user = SeedUser(repo);
        TestableUserManager svc = NewService(repo);

        IResponse<IntUser> byEmail = await svc.GetUser(Email);
        IResponse<IntUser> byUsername = await svc.GetUser(Username);
        IResponse<IntUser> byPhone = await svc.GetUser(Phone.ToString());

        byEmail.StatusCode.Should().Be(StatusCode.Succeeded);
        byEmail.Data!.Id.Should().Be(user.Id);
        byUsername.StatusCode.Should().Be(StatusCode.Succeeded);
        byUsername.Data!.Id.Should().Be(user.Id);
        byPhone.StatusCode.Should().Be(StatusCode.Succeeded);
        byPhone.Data!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetUser_By_String_Empty_Returns_Failed_InvalidUserIdentifier()
    {
        TestableUserManager svc = NewService();

        IResponse<IntUser> result = await svc.GetUser(string.Empty);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(Identity_Messages.InvalidUserIdentifier);
    }

    [Fact]
    public async Task GetUser_By_String_NotFound_Returns_NotExists_UserNotFound()
    {
        TestableUserManager svc = NewService();

        IResponse<IntUser> result = await svc.GetUser("nobody@example.com");

        result.StatusCode.Should().Be(StatusCode.NotExists);
        result.UserMessage.Should().Be(Identity_Messages.UserNotFound);
    }

    [Fact]
    public async Task GetUser_By_Id_Found_And_NotFound()
    {
        IntUserRepository repo = NewRepo();
        IntUser user = SeedUser(repo);
        TestableUserManager svc = NewService(repo);

        IResponse<IntUser> found = await svc.GetUser(user.Id);
        IResponse<IntUser> notFound = await svc.GetUser(user.Id + 999);

        found.StatusCode.Should().Be(StatusCode.Succeeded);
        found.Data!.Id.Should().Be(user.Id);
        notFound.StatusCode.Should().Be(StatusCode.NotExists);
        notFound.UserMessage.Should().Be(Identity_Messages.UserNotFound);
    }

    [Fact]
    public async Task Login_Unknown_Id_Returns_NotExists_UserNotFound()
    {
        TestableUserManager svc = NewService();

        IResponse<string> result = await svc.Login(9999L);

        result.StatusCode.Should().Be(StatusCode.NotExists);
        result.UserMessage.Should().Be(Identity_Messages.UserNotFound);
    }

    [Fact]
    public async Task Login_Generates_Token_With_Roles_Claims_And_MustChangePassword()
    {
        IntUserRepository repo = NewRepo();
        IntUser user = SeedUser(repo, mustChangePassword: true);
        await repo.AddUserRoleAsync(new IntUserRole { UserId = user.Id, RoleId = RoleId });
        IntClaim claim = new IntClaim
        {
            Id = 5,
            ControllerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ActionType = (byte)ClaimActionTypes.Get
        };
        repo.AddClaim(claim);
        await repo.AddUserClaimAsync(new IntUserClaim { UserId = user.Id, ClaimId = claim.Id });
        TestableUserManager svc = NewService(repo);

        IResponse<string> result = await svc.Login(user.Id);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().NotBeNullOrEmpty();
        JwtSecurityToken jwt = ReadJwt(result.Data);
        jwt.Claims.Should().Contain(c => c.Type == SdClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == SdClaimTypes.Roles && c.Value == RoleId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == SdClaimTypes.ControllerAction
            && c.Value == $"{claim.ControllerId}:{claim.ActionType}");
        jwt.Claims.Should().Contain(c => c.Type == SdClaimTypes.MustChangePassword && c.Value == "true");
    }

    [Fact]
    public async Task RefreshToken_Unknown_User_Returns_NotExists()
    {
        TestableUserManager svc = NewService();

        IResponse<string> result = await svc.RefreshToken(9999L);

        result.StatusCode.Should().Be(StatusCode.NotExists);
    }

    [Fact]
    public async Task RefreshToken_Generates_Token_Without_Forcing_MustChangePassword()
    {
        IntUserRepository repo = NewRepo();
        IntUser user = SeedUser(repo, mustChangePassword: true);
        await repo.AddUserRoleAsync(new IntUserRole { UserId = user.Id, RoleId = RoleId });
        TestableUserManager svc = NewService(repo);

        IResponse<string> result = await svc.RefreshToken(user.Id);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().NotBeNullOrEmpty();
        JwtSecurityToken jwt = ReadJwt(result.Data);
        jwt.Claims.Should().Contain(c => c.Type == SdClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == SdClaimTypes.Roles && c.Value == RoleId.ToString());
        jwt.Claims.Should().NotContain(c => c.Type == SdClaimTypes.MustChangePassword);
    }

    [Fact]
    public async Task RegisterUser_Duplicate_Username_Returns_Duplicate_DuplicateUsername()
    {
        IntUserRepository repo = NewRepo();
        SeedUser(repo, username: Username);
        TestableUserManager svc = NewService(repo);
        RegisterVM<byte> model = new RegisterVM<byte>
        {
            Username = Username,
            Password = Password,
            Roles = new List<byte> { RoleId }
        };

        IResponse<long> result = await svc.RegisterUser(model);

        result.StatusCode.Should().Be(StatusCode.Duplicate);
        result.UserMessage.Should().Be(Identity_Messages.DuplicateUsername);
    }

    [Fact]
    public async Task RegisterUser_Hashes_Password_Never_Stores_Plaintext()
    {
        IntUserRepository repo = NewRepo();
        TestableUserManager svc = NewService(repo);
        RegisterVM<byte> model = new RegisterVM<byte>
        {
            Username = "newbie",
            Password = Password,
            Roles = new List<byte> { RoleId }
        };

        IResponse<long> result = await svc.RegisterUser(model);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        IntUser stored = repo.Storage.Single(u => u.Username == "newbie");
        stored.HashedPassword.Should().NotBe(Password);
        stored.HashedPassword.Should().NotBeNullOrEmpty();
        Utilities.VerifyPassword(Password, stored.HashedPassword).Should().BeTrue();
        Utilities.VerifyPassword("wrong-password", stored.HashedPassword).Should().BeFalse();
    }

    [Fact]
    public async Task RegisterUser_Assigns_All_Roles()
    {
        IntUserRepository repo = NewRepo();
        TestableUserManager svc = NewService(repo);
        byte[] roles = new byte[] { 1, 2, 3 };
        RegisterVM<byte> model = new RegisterVM<byte>
        {
            Username = "multirole",
            Password = Password,
            Roles = roles.ToList()
        };

        IResponse<long> result = await svc.RegisterUser(model);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        IEnumerable<byte> assignedRoles = repo.FetchAllUserRoles()
            .Where(ur => ur.UserId.Equals(result.Data))
            .Select(ur => ur.RoleId)
            .OrderBy(r => r);
        assignedRoles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task RegisterUser_On_Failure_Propagates_Exception_Status()
    {
        FailingUserRepository failingRepo = new FailingUserRepository();
        TestableUserManager svc = new TestableUserManager(failingRepo, CreateMapper(), TokenService());
        RegisterVM<byte> model = new RegisterVM<byte>
        {
            Username = "failuser",
            Password = Password,
            Roles = new List<byte> { RoleId }
        };

        IResponse<long> result = await svc.RegisterUser(model);

        result.StatusCode.Should().Be(StatusCode.Exception);
    }

    [Fact]
    public async Task VerifyUser_Numeric_Recipient_Sets_VerifiedPhoneNumber()
    {
        IntUserRepository repo = NewRepo();
        IntUser user = SeedUser(repo);
        TestableUserManager svc = NewService(repo);

        IResponse result = await svc.VerifyUser(Phone.ToString());

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        IntUser stored = repo.Storage.Single(u => u.Id == user.Id);
        stored.VerifiedPhoneNumber.Should().BeTrue();
        stored.VerifiedEmail.Should().BeFalse();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task VerifyUser_Email_Recipient_Sets_VerifiedEmail()
    {
        IntUserRepository repo = NewRepo();
        IntUser user = SeedUser(repo);
        TestableUserManager svc = NewService(repo);

        IResponse result = await svc.VerifyUser(Email);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        IntUser stored = repo.Storage.Single(u => u.Id == user.Id);
        stored.VerifiedEmail.Should().BeTrue();
        stored.VerifiedPhoneNumber.Should().BeFalse();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task VerifyUser_NotFound_Returns_NotExists()
    {
        TestableUserManager svc = NewService();

        IResponse result = await svc.VerifyUser("unknown@example.com");

        result.StatusCode.Should().Be(StatusCode.NotExists);
        result.UserMessage.Should().Be(Identity_Messages.UserNotFound);
    }

    [Fact]
    public async Task VerifyUser_Empty_Returns_Failed_InvalidEmailOrNumber()
    {
        TestableUserManager svc = NewService();

        IResponse result = await svc.VerifyUser(string.Empty);

        result.StatusCode.Should().Be(StatusCode.Failed);
        result.UserMessage.Should().Be(Identity_Messages.InvalidEmailOrNumber);
    }

    [Fact]
    public async Task Edit_NotFound_Returns_NotExists()
    {
        TestableUserManager svc = NewService();

        IResponse result = await svc.Edit(9999L, new UserEditableVM
        {
            Username = "ghost",
            Password = Password,
            Roles = new List<byte>(),
            Claims = new List<byte>()
        });

        result.StatusCode.Should().Be(StatusCode.NotExists);
    }

    [Fact]
    public async Task Edit_Updates_Fields_And_Syncs_Roles_And_Claims()
    {
        IntUserRepository repo = NewRepo();
        IntUser user = SeedUser(repo);
        await repo.AddUserRoleAsync(new IntUserRole { UserId = user.Id, RoleId = 1 });
        await repo.AddUserRoleAsync(new IntUserRole { UserId = user.Id, RoleId = 2 });
        IntClaim claim1 = new IntClaim { Id = 10, ControllerId = Guid.NewGuid(), ActionType = (byte)ClaimActionTypes.Get };
        IntClaim claim2 = new IntClaim { Id = 11, ControllerId = Guid.NewGuid(), ActionType = (byte)ClaimActionTypes.Add };
        repo.AddClaim(claim1);
        repo.AddClaim(claim2);
        repo.AddClaim(new IntClaim { Id = 12, ControllerId = Guid.NewGuid(), ActionType = (byte)ClaimActionTypes.Delete });
        await repo.AddUserClaimAsync(new IntUserClaim { UserId = user.Id, ClaimId = 10 });
        await repo.AddUserClaimAsync(new IntUserClaim { UserId = user.Id, ClaimId = 11 });
        TestableUserManager svc = NewService(repo);

        IResponse result = await svc.Edit(user.Id, new UserEditableVM
        {
            Username = "alice-renamed",
            Password = Password,
            Roles = new List<byte> { 2, 3 },
            Claims = new List<byte> { 11, 12 }
        });

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        IntUser stored = repo.Storage.Single(u => u.Id == user.Id);
        stored.Username.Should().Be("alice-renamed");
        IEnumerable<byte> finalRoles = repo.FetchAllUserRoles()
            .Where(ur => ur.UserId.Equals(user.Id))
            .Select(ur => ur.RoleId)
            .OrderBy(r => r);
        finalRoles.Should().BeEquivalentTo(new byte[] { 2, 3 });
        IEnumerable<byte> finalClaims = repo.FetchAllUserClaims()
            .Where(uc => uc.UserId.Equals(user.Id))
            .Select(uc => uc.ClaimId)
            .OrderBy(c => c);
        finalClaims.Should().BeEquivalentTo(new byte[] { 11, 12 });
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ChangePassword_NotFound_Returns_NotExists()
    {
        TestableUserManager svc = NewService();

        IResponse result = await svc.ChangePassword(9999L, "new-password");

        result.StatusCode.Should().Be(StatusCode.NotExists);
        result.UserMessage.Should().Be(Identity_Messages.UserNotFound);
    }

    [Fact]
    public async Task ChangePassword_Updates_Hash_And_Clears_MustChangePassword()
    {
        IntUserRepository repo = NewRepo();
        IntUser user = SeedUser(repo, mustChangePassword: true);
        string originalHash = user.HashedPassword;
        TestableUserManager svc = NewService(repo);
        const string newPassword = "Brand-new-pw-1";

        IResponse result = await svc.ChangePassword(user.Id, newPassword);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        IntUser stored = repo.Storage.Single(u => u.Id == user.Id);
        stored.HashedPassword.Should().NotBe(originalHash);
        stored.HashedPassword.Should().NotBe(newPassword);
        stored.MustChangePassword.Should().BeFalse();
        Utilities.VerifyPassword(newPassword, stored.HashedPassword).Should().BeTrue();
        Utilities.VerifyPassword(Password, stored.HashedPassword).Should().BeFalse();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task DeleteUser_NotFound_Returns_NotExists()
    {
        TestableUserManager svc = NewService();

        IResponse result = await svc.DeleteUser(9999L);

        result.StatusCode.Should().Be(StatusCode.NotExists);
    }

    [Fact]
    public async Task DeleteUser_Success_Deletes()
    {
        IntUserRepository repo = NewRepo();
        IntUser user = SeedUser(repo);
        TestableUserManager svc = NewService(repo);

        IResponse result = await svc.DeleteUser(user.Id);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        repo.Storage.Should().NotContain(u => u.Id == user.Id);
        IResponse<IntUser> fetchResult = await svc.GetUser(user.Id);
        fetchResult.StatusCode.Should().Be(StatusCode.NotExists);
    }
}
