using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Sardanapal.Identity.Authorization.Data;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.ViewModel.Response;
using Xunit;

namespace Sardanapal.Identity.Authorization.Data.Tests.Unit;

public class IdentityProviderTests
{
    private const string Uid = "user-42";
    private const string Token = "header.payload.signature";

    private static IResponse<bool> OkValidation() => new Response<bool>(NullLogger.Instance)
    {
        StatusCode = StatusCode.Succeeded,
        Data = true
    };

    private static IResponse<bool> InvalidValidation() => new Response<bool>(NullLogger.Instance)
    {
        StatusCode = StatusCode.Exception,
        Data = false
    };

    private static ClaimsPrincipal PrincipalWithId(string uid) => new ClaimsPrincipal(
        new ClaimsIdentity(new[]
        {
            new Claim(SdClaimTypes.NameIdentifier, uid),
            new Claim(SdClaimTypes.Roles, "1")
        }, "Test"));

    private static ClaimsPrincipal PrincipalWithoutId() => new ClaimsPrincipal(
        new ClaimsIdentity(new[]
        {
            new Claim(SdClaimTypes.Roles, "1")
        }, "Test"));

    private static ITokenService TokenServiceReturning(IResponse<bool> response, ClaimsPrincipal principal)
    {
        ITokenService tokenService = Substitute.For<ITokenService>();
        tokenService.ValidateToken(Arg.Any<string>(), out Arg.Any<ClaimsPrincipal>())
            .Returns(callInfo =>
            {
                callInfo[1] = principal;
                return response;
            });
        return tokenService;
    }

    [Fact]
    public void SetAnonymous_Sets_IsAnonymous_True()
    {
        IdentityProvider provider = new IdentityProvider(Substitute.For<ITokenService>());

        provider.SetAnonymous();

        provider.IsAnonymous.Should().BeTrue();
    }

    [Fact]
    public void SetAuthorize_Parameterless_Sets_IsAuthorized_True()
    {
        IdentityProvider provider = new IdentityProvider(Substitute.For<ITokenService>());

        provider.SetAuthorize();

        provider.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public void SetAuthorize_Valid_Token_Sets_Authorized_Token_Claims_UserId()
    {
        ClaimsPrincipal principal = PrincipalWithId(Uid);
        ITokenService tokenService = TokenServiceReturning(OkValidation(), principal);
        IdentityProvider provider = new IdentityProvider(tokenService);

        provider.SetAuthorize(Token);

        provider.IsAuthorized.Should().BeTrue();
        provider.Token.Should().Be(Token);
        provider.Claims.Should().BeSameAs(principal);
        provider.Claims.Should().NotBeNull();
        provider.Claims!.FindFirst(SdClaimTypes.NameIdentifier)!.Value.Should().Be(Uid);
        provider.UserId.Should().Be(Uid);
    }

    [Fact]
    public void SetAuthorize_Valid_Token_Without_NameIdentifier_Leaves_UserId_Null()
    {
        ClaimsPrincipal principal = PrincipalWithoutId();
        ITokenService tokenService = TokenServiceReturning(OkValidation(), principal);
        IdentityProvider provider = new IdentityProvider(tokenService);

        provider.SetAuthorize(Token);

        provider.IsAuthorized.Should().BeTrue();
        provider.Token.Should().Be(Token);
        provider.Claims.Should().BeSameAs(principal);
        provider.UserId.Should().BeNull();
    }

    [Fact]
    public void SetAuthorize_Invalid_Token_Leaves_State_Unchanged()
    {
        ClaimsPrincipal principal = PrincipalWithId(Uid);
        ITokenService tokenService = TokenServiceReturning(InvalidValidation(), principal);
        IdentityProvider provider = new IdentityProvider(tokenService);

        provider.SetAuthorize(Token);

        provider.IsAuthorized.Should().BeFalse();
        provider.IsAnonymous.Should().BeFalse();
        provider.Token.Should().BeNull();
        provider.Claims.Should().BeNull();
        provider.UserId.Should().BeNull();
    }
}
