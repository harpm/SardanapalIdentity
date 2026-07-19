using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sardanapal.Contract.IModel;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Services.Services;
using Sardanapal.Identity.Share.Static;
using Sardanapal.Identity.Share.Types;
using Sardanapal.ViewModel.Response;
using Xunit;

namespace Sardanapal.Identity.Services.Tests.Unit;

internal sealed class TestAccessRightClaim : IClaim<byte>
{
    public byte ClaimType => (byte)SdClaimType.AccessRight;
    public byte Id { get; set; }
}

internal sealed class TestControllerActionClaim : IControllerActionClaim<byte>
{
    public byte ClaimType => (byte)SdClaimType.ControllerAction;
    public byte Id { get; set; }
    public Guid ControllerId { get; set; }
    public byte ActionType { get; set; }
}

internal sealed class TestUnknownClaim : IClaim
{
    public byte ClaimType { get; set; } = 99;
}

internal sealed class TestableTokenService : TokenService
{
    public TestableTokenService(IOptions<SDConfigs> config, ILogger logger)
        : base(config, logger)
    {
    }

    public IEnumerable<Claim> MapTokenClaimsPublic(IClaim[] claims) => MapTokenClaims(claims);

    public bool HasClaimsPublic(ClaimsPrincipal principal, IClaim[] claims) => HasClaims(principal, claims);

    public string GenerateTokenDirect(string uid, int expireTime, byte[] roleIds, IClaim[] claims, bool mustChangePassword = false)
        => GenerateToken(uid, expireTime, roleIds, claims, mustChangePassword);
}

public class TokenServiceTests
{
    private const string Uid = "abc-123";

    private static SymmetricSecurityKey Key =>
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-key-at-least-32-bytes-long-xxxxxxxx"));

    private static TokenValidationParameters ValidParameters() => new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = Key,
        ClockSkew = TimeSpan.Zero
    };

    private static SDConfigs DefaultConfig(int expiration = 30) => new SDConfigs
    {
        ExpirationTime = expiration,
        TokenParameters = ValidParameters()
    };

    private static TestableTokenService CreateService(int expiration = 30) =>
        new TestableTokenService(Options.Create(DefaultConfig(expiration)), NullLogger.Instance);

    private static JwtSecurityToken ReadJwt(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    [Fact]
    public void Constructor_Null_Config_Throws_ArgumentNullException()
    {
        Action act = () => new TokenService(null!, NullLogger.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(SDConfigs));
    }

    [Fact]
    public void GenerateToken_Returns_NonEmpty_JWT_String()
    {
        TestableTokenService service = CreateService();

        IResponse<string> result = service.GenerateToken(Uid, Array.Empty<byte>(), Array.Empty<IClaim>());

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().NotBeNullOrEmpty();
        result.Data.Split('.').Should().HaveCount(3, "a JWT has three dot-separated parts");
    }

    [Fact]
    public void GenerateToken_Null_TokenParameters_Throws()
    {
        SDConfigs config = new SDConfigs { ExpirationTime = 30, TokenParameters = null! };
        TestableTokenService service = new TestableTokenService(Options.Create(config), NullLogger.Instance);

        Action act = () => service.GenerateTokenDirect(Uid, 30, Array.Empty<byte>(), Array.Empty<IClaim>());

        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void GenerateToken_Sets_NameIdentifier_Claim_Equal_Uid()
    {
        TestableTokenService service = CreateService();

        IResponse<string> result = service.GenerateToken(Uid, Array.Empty<byte>(), Array.Empty<IClaim>());

        JwtSecurityToken jwt = ReadJwt(result.Data);
        jwt.Claims.Should().Contain(c => c.Type == SdClaimTypes.NameIdentifier && c.Value == Uid);
    }

    [Fact]
    public void GenerateToken_Adds_One_Roles_Claim_Per_RoleId()
    {
        byte[] roleIds = new byte[] { 1, 2, 3 };
        TestableTokenService service = CreateService();

        IResponse<string> result = service.GenerateToken(Uid, roleIds, Array.Empty<IClaim>());

        JwtSecurityToken jwt = ReadJwt(result.Data);
        jwt.Claims.Where(c => c.Type == SdClaimTypes.Roles).Select(c => c.Value)
            .Should().BeEquivalentTo(new[] { "1", "2", "3" });
    }

    [Fact]
    public void GenerateToken_Adds_MustChangePassword_True_Only_When_Flag_Set()
    {
        TestableTokenService service = CreateService();

        IResponse<string> withoutFlag = service.GenerateToken(Uid, Array.Empty<byte>(), Array.Empty<IClaim>(), mustChangePassword: false);
        IResponse<string> withFlag = service.GenerateToken(Uid, Array.Empty<byte>(), Array.Empty<IClaim>(), mustChangePassword: true);

        JwtSecurityToken jwtWithout = ReadJwt(withoutFlag.Data);
        JwtSecurityToken jwtWith = ReadJwt(withFlag.Data);

        jwtWithout.Claims.Should().NotContain(c => c.Type == SdClaimTypes.MustChangePassword);
        jwtWith.Claims.Should().Contain(c => c.Type == SdClaimTypes.MustChangePassword && c.Value == "true");
    }

    [Fact]
    public void GenerateToken_Formats_ControllerAction_Claims_As_ControllerId_Colon_ActionType()
    {
        Guid controllerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        IControllerActionClaim<byte> claim = new TestControllerActionClaim
        {
            Id = 7,
            ControllerId = controllerId,
            ActionType = (byte)ClaimActionTypes.Add
        };
        TestableTokenService service = CreateService();

        IResponse<string> result = service.GenerateToken(Uid, Array.Empty<byte>(), new IClaim[] { claim });

        JwtSecurityToken jwt = ReadJwt(result.Data);
        jwt.Claims.Should().Contain(c => c.Type == SdClaimTypes.ControllerAction
            && c.Value == $"{controllerId}:{(byte)ClaimActionTypes.Add}");
    }

    [Fact]
    public void GenerateToken_Formats_AccessRight_Claims_As_Id_String()
    {
        IClaim<byte> claim = new TestAccessRightClaim { Id = 5 };
        TestableTokenService service = CreateService();

        IResponse<string> result = service.GenerateToken(Uid, Array.Empty<byte>(), new IClaim[] { claim });

        JwtSecurityToken jwt = ReadJwt(result.Data);
        jwt.Claims.Should().Contain(c => c.Type == SdClaimTypes.AccessRights && c.Value == "5");
    }

    [Fact]
    public void GenerateToken_Uses_HmacSha256()
    {
        TestableTokenService service = CreateService();

        IResponse<string> result = service.GenerateToken(Uid, Array.Empty<byte>(), Array.Empty<IClaim>());

        JwtSecurityToken jwt = ReadJwt(result.Data);
        jwt.Header.Alg.Should().Be("HS256");
    }

    [Fact]
    public void GenerateToken_Sets_Expiration_To_Now_Plus_ExpirationTime()
    {
        const int expirationMinutes = 45;
        TestableTokenService service = CreateService(expirationMinutes);
        DateTime before = DateTime.UtcNow;

        IResponse<string> result = service.GenerateToken(Uid, Array.Empty<byte>(), Array.Empty<IClaim>());

        DateTime after = DateTime.UtcNow;
        JwtSecurityToken jwt = ReadJwt(result.Data);
        jwt.ValidTo.Should().BeCloseTo(before.AddMinutes(expirationMinutes), TimeSpan.FromSeconds(5));
        jwt.ValidTo.Should().BeAfter(after.AddMinutes(expirationMinutes).AddSeconds(-5));
    }

    [Fact]
    public void GenerateToken_Null_Claims_Array_Does_Not_Throw()
    {
        TestableTokenService service = CreateService();

        Action act = () => service.GenerateToken(Uid, Array.Empty<byte>(), null!);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateToken_Valid_Token_Succeeds_And_Populates_Claims()
    {
        TestableTokenService service = CreateService();
        IResponse<string> generated = service.GenerateToken(Uid, new byte[] { 1 }, Array.Empty<IClaim>());

        IResponse<bool> result = service.ValidateToken(generated.Data, out ClaimsPrincipal principal);

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().BeTrue();
        principal.Should().NotBeNull();
        principal.Claims.Should().NotBeEmpty();
        principal.Claims.Should().Contain(c => c.Type == SdClaimTypes.NameIdentifier && c.Value == Uid);
    }

    [Fact]
    public void ValidateToken_Invalid_Token_Returns_Exception_Status()
    {
        TestableTokenService service = CreateService();

        IResponse<bool> result = service.ValidateToken("not.a.valid.jwt", out ClaimsPrincipal principal);

        result.StatusCode.Should().Be(StatusCode.Exception);
    }

    [Fact]
    public void ValidateToken_Expired_Token_Returns_Exception_Status()
    {
        TestableTokenService service = CreateService(expiration: -10);

        IResponse<string> generated = service.GenerateToken(Uid, Array.Empty<byte>(), Array.Empty<IClaim>());

        IResponse<bool> result = service.ValidateToken(generated.Data, out ClaimsPrincipal _);

        result.StatusCode.Should().Be(StatusCode.Exception);
    }

    [Fact]
    public void ValidateToken_Tampered_Token_Returns_Exception_Status()
    {
        TestableTokenService service = CreateService();
        IResponse<string> generated = service.GenerateToken(Uid, Array.Empty<byte>(), Array.Empty<IClaim>());
        char[] chars = generated.Data.ToCharArray();
        chars[chars.Length - 3] = chars[chars.Length - 3] == 'A' ? 'B' : 'A';
        string tampered = new string(chars);

        IResponse<bool> result = service.ValidateToken(tampered, out ClaimsPrincipal _);

        result.StatusCode.Should().Be(StatusCode.Exception);
    }

    [Fact]
    public void ValidateTokenRoles_Succeeds_When_Role_And_Claim_Present()
    {
        IControllerActionClaim<byte> claim = new TestControllerActionClaim
        {
            Id = 1,
            ControllerId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            ActionType = (byte)ClaimActionTypes.Get
        };
        TestableTokenService service = CreateService();
        IResponse<string> generated = service.GenerateToken(Uid, new byte[] { 3 }, new IClaim[] { claim });

        IResponse<bool> result = service.ValidateTokenRoles(generated.Data, new byte[] { 3 }, new IClaim[] { claim });

        result.StatusCode.Should().Be(StatusCode.Succeeded);
        result.Data.Should().BeTrue();
    }

    [Fact]
    public void ValidateTokenRoles_Fails_Missing_Role()
    {
        TestableTokenService service = CreateService();
        IResponse<string> generated = service.GenerateToken(Uid, new byte[] { 1 }, Array.Empty<IClaim>());

        IResponse<bool> result = service.ValidateTokenRoles(generated.Data, new byte[] { 9 }, Array.Empty<IClaim>());

        result.StatusCode.Should().Be(StatusCode.NotExists);
        result.Data.Should().BeFalse();
    }

    [Fact]
    public void ValidateTokenRoles_Fails_Missing_Claim()
    {
        IControllerActionClaim<byte> required = new TestControllerActionClaim
        {
            Id = 1,
            ControllerId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ActionType = (byte)ClaimActionTypes.Delete
        };
        TestableTokenService service = CreateService();
        IResponse<string> generated = service.GenerateToken(Uid, new byte[] { 1 }, Array.Empty<IClaim>());

        IResponse<bool> result = service.ValidateTokenRoles(generated.Data, new byte[] { 1 }, new IClaim[] { required });

        result.StatusCode.Should().Be(StatusCode.NotExists);
        result.Data.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    public void ValidateTokenRoles_Succeeds_When_Claims_Null_Or_Empty(IClaim[] claims)
    {
        TestableTokenService service = CreateService();
        IResponse<string> generated = service.GenerateToken(Uid, new byte[] { 1 }, Array.Empty<IClaim>());

        IResponse<bool> result = service.ValidateTokenRoles(generated.Data, new byte[] { 1 }, claims ?? Array.Empty<IClaim>());

        result.StatusCode.Should().Be(StatusCode.Succeeded);
    }

    [Fact]
    public void HasClaims_Skips_Null_Claims_And_Unknown_ClaimType()
    {
        TestableTokenService service = CreateService();
        IResponse<string> generated = service.GenerateToken(Uid, Array.Empty<byte>(), Array.Empty<IClaim>());
        generated.StatusCode.Should().Be(StatusCode.Succeeded);
        ClaimsPrincipal principal = new JwtSecurityTokenHandler().ValidateToken(
            generated.Data, ValidParameters(), out SecurityToken _);

        bool result = service.HasClaimsPublic(principal, new IClaim[] { null!, new TestUnknownClaim() });

        result.Should().BeFalse();
    }

    [Fact]
    public void MapTokenClaims_Null_Returns_Empty()
    {
        TestableTokenService service = CreateService();

        IEnumerable<Claim> result = service.MapTokenClaimsPublic(null!);

        result.Should().BeEmpty();
    }

    [Fact]
    public void MapTokenClaims_Skips_Null_Entries()
    {
        IClaim<byte> valid = new TestAccessRightClaim { Id = 4 };
        TestableTokenService service = CreateService();

        List<Claim> result = service.MapTokenClaimsPublic(new IClaim[] { null!, valid, null! }).ToList();

        result.Should().HaveCount(1);
        result[0].Type.Should().Be(SdClaimTypes.AccessRights);
        result[0].Value.Should().Be("4");
    }

    [Fact]
    public void RoundTrip_Generate_Then_Validate_Succeeds()
    {
        TestableTokenService service = CreateService();

        IResponse<string> generated = service.GenerateToken(Uid, new byte[] { 2 }, Array.Empty<IClaim>());
        IResponse<bool> validated = service.ValidateToken(generated.Data, out ClaimsPrincipal principal);

        validated.StatusCode.Should().Be(StatusCode.Succeeded);
        validated.Data.Should().BeTrue();
        principal.Claims.Should().Contain(c => c.Type == SdClaimTypes.NameIdentifier && c.Value == Uid);
    }
}
