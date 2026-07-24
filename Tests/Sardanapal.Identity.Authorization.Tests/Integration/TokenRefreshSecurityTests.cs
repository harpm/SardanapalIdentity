using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Sardanapal.Identity.Authorization.Middlewares;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.ViewModel.Response;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Sardanapal.Identity.Authorization.Tests.Integration;

public class TokenRefreshSecurityTests
{
    private const string Uid = "sec-user-1";

    private static ClaimsPrincipal Principal(int expMinutes, bool mustChange)
    {
        List<Claim> claims = new List<Claim>
        {
            new Claim(SdClaimTypes.NameIdentifier, Uid),
            new Claim(SdClaimTypes.Roles, "1"),
            new Claim("exp", new DateTimeOffset(DateTime.UtcNow.AddMinutes(expMinutes)).ToUnixTimeSeconds().ToString())
        };
        if (mustChange)
            claims.Add(new Claim(SdClaimTypes.MustChangePassword, "true"));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static async Task<(bool generated, bool mustChangeArg)> RunRefresh(int threshold, ClaimsPrincipal principal)
    {
        bool generated = false;
        bool mustChangeArg = false;
        ITokenService tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateToken(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<IClaim[]>(), Arg.Do<bool>(b => mustChangeArg = b))
            .Returns(callInfo =>
            {
                generated = true;
                return new Response<string>(NullLogger.Instance) { StatusCode = StatusCode.Succeeded, Data = "new-token" };
            });

        IIdentityProvider provider = Substitute.For<IIdentityProvider>();
        provider.IsAuthorized.Returns(true);
        provider.Claims.Returns(principal);

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(provider);
        services.AddSingleton<IOptions<SDConfigs>>(Options.Create(new SDConfigs { TokenRefreshThresholdMinutes = threshold }));
        services.AddSingleton(tokenService);
        DefaultHttpContext context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Headers[ConstantKeys.AUTH_HEADER_KEY] = "old.token";

        SdAuthorizationMiddlewareWithRefreshToken middleware =
            new SdAuthorizationMiddlewareWithRefreshToken(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context, provider);

        return (generated, mustChangeArg);
    }

    [Fact]
    public async Task Token_Refresh_Only_Near_Expiry_And_Preserves_MustChangePassword()
    {
        var (generatedFar, _) = await RunRefresh(threshold: 5, Principal(expMinutes: 60, mustChange: true));
        generatedFar.Should().BeFalse("a token far from expiry must not be refreshed");

        var (generatedNear, mustChange) = await RunRefresh(threshold: 5, Principal(expMinutes: 1, mustChange: true));
        generatedNear.Should().BeTrue("a token within the threshold window must be refreshed");
        mustChange.Should().BeTrue("the must_change_pw flag must survive the refresh");

        var (generatedNoFlag, mustChange2) = await RunRefresh(threshold: 5, Principal(expMinutes: 1, mustChange: false));
        generatedNoFlag.Should().BeTrue();
        mustChange2.Should().BeFalse("refresh must not fabricate a must_change_pw flag that was absent");
    }
}
