using System.Globalization;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Sardanapal.Identity.Authorization.Middlewares;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.ViewModel.Response;
using Xunit;
using static Sardanapal.Identity.Share.Types.SdClaimType;

namespace Sardanapal.Identity.Authorization.Tests.Integration;

public class SdAuthorizationMiddlewareWithRefreshTokenTests
{
    private const string Uid = "user-7";
    private const string RefreshedToken = "refreshed.token.value";

    private static IOptions<SDConfigs> Config(int threshold) =>
        Options.Create(new SDConfigs { TokenRefreshThresholdMinutes = threshold });

    private static ClaimsPrincipal AuthorizedPrincipal(
        string? uid = Uid,
        byte[]? roles = null,
        IEnumerable<Claim>? extraClaims = null,
        string? expClaimValue = null,
        string? expClaimType = null,
        bool mustChange = false)
    {
        List<Claim> claims = new List<Claim>();
        if (uid != null) claims.Add(new Claim(SdClaimTypes.NameIdentifier, uid));
        foreach (byte role in roles ?? Array.Empty<byte>())
            claims.Add(new Claim(SdClaimTypes.Roles, role.ToString()));
        if (extraClaims != null) claims.AddRange(extraClaims);
        if (expClaimValue != null)
            claims.Add(new Claim(expClaimType ?? "exp", expClaimValue));
        if (mustChange)
            claims.Add(new Claim(SdClaimTypes.MustChangePassword, "true"));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static (DefaultHttpContext context, ITokenService tokenService, IIdentityProvider provider, CapturedArgs captured)
        BuildSetup(SDConfigs config, ClaimsPrincipal principal)
    {
        ITokenService tokenService = Substitute.For<ITokenService>();
        IIdentityProvider provider = Substitute.For<IIdentityProvider>();
        provider.IsAuthorized.Returns(true);
        provider.Claims.Returns(principal);

        CapturedArgs captured = new CapturedArgs();
        tokenService.GenerateToken(
                Arg.Do<string>(u => captured.Uid = u),
                Arg.Do<byte[]>(r => captured.Roles = r),
                Arg.Do<IClaim[]>(c => captured.Claims = c),
                Arg.Do<bool>(m => captured.MustChange = m))
            .Returns(new Response<string>(NullLogger.Instance)
            {
                StatusCode = StatusCode.Succeeded,
                Data = RefreshedToken
            });

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(provider);
        services.AddSingleton<IOptions<SDConfigs>>(Options.Create(config));
        services.AddSingleton(tokenService);

        DefaultHttpContext context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Headers[ConstantKeys.AUTH_HEADER_KEY] = "expiring.token";

        return (context, tokenService, provider, captured);
    }

    private static Task Invoke(DefaultHttpContext context, IIdentityProvider provider)
    {
        SdAuthorizationMiddlewareWithRefreshToken middleware =
            new SdAuthorizationMiddlewareWithRefreshToken(_ => Task.CompletedTask);
        return middleware.InvokeAsync(context, provider);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RefreshMiddleware_Threshold_Zero_Or_Negative_Disabled(int threshold)
    {
        var (context, tokenService, provider, _) =
            BuildSetup(new SDConfigs { TokenRefreshThresholdMinutes = threshold },
                AuthorizedPrincipal(expClaimValue: FutureUnix(0)));

        await Invoke(context, provider);

        tokenService.DidNotReceiveWithAnyArgs().GenerateToken(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<IClaim[]>(), Arg.Any<bool>());
        context.Response.Headers.ContainsKey(ConstantKeys.AUTH_HEADER_KEY).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshMiddleware_Far_From_Expiry_No_Refresh()
    {
        var (context, tokenService, provider, _) =
            BuildSetup(new SDConfigs { TokenRefreshThresholdMinutes = 5 },
                AuthorizedPrincipal(expClaimValue: FutureUnix(60)));

        await Invoke(context, provider);

        tokenService.DidNotReceiveWithAnyArgs().GenerateToken(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<IClaim[]>(), Arg.Any<bool>());
        context.Response.Headers.ContainsKey(ConstantKeys.AUTH_HEADER_KEY).Should().BeFalse();
    }

    private static string AuthHeader(HttpContext ctx) => (string)ctx.Response.Headers[ConstantKeys.AUTH_HEADER_KEY];

    [Fact]
    public async Task RefreshMiddleware_Near_Expiry_Reissues_Token_In_Response_Header()
    {
        var (context, tokenService, provider, _) =
            BuildSetup(new SDConfigs { TokenRefreshThresholdMinutes = 10 },
                AuthorizedPrincipal(roles: new byte[] { 3 }, expClaimValue: FutureUnix(2)));

        await Invoke(context, provider);

        AuthHeader(context).Should().Be(RefreshedToken);
    }

    [Fact]
    public async Task RefreshMiddleware_Preserves_MustChangePassword()
    {
        var (context, tokenService, provider, captured) =
            BuildSetup(new SDConfigs { TokenRefreshThresholdMinutes = 10 },
                AuthorizedPrincipal(expClaimValue: FutureUnix(1), mustChange: true));

        await Invoke(context, provider);

        AuthHeader(context).Should().Be(RefreshedToken);
        captured.MustChange.Should().BeTrue();
        captured.Uid.Should().Be(Uid);
    }

    [Fact]
    public async Task RefreshMiddleware_Reconstructs_AccessRight_And_ControllerAction_Claims()
    {
        Guid controllerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        List<Claim> extra = new List<Claim>
        {
            new Claim(SdClaimTypes.AccessRights, "5"),
            new Claim(SdClaimTypes.ControllerAction, $"{controllerId}:2")
        };
        var (context, tokenService, provider, captured) =
            BuildSetup(new SDConfigs { TokenRefreshThresholdMinutes = 10 },
                AuthorizedPrincipal(expClaimValue: FutureUnix(1), extraClaims: extra));

        await Invoke(context, provider);

        captured.Claims.Should().NotBeNull();
        IClaim[]? claims = captured.Claims;
        claims.Should().HaveCount(2);
        claims!.Where(c => c.ClaimType == (byte)AccessRight).Should().HaveCount(1);
        claims.Where(c => c.ClaimType == (byte)ControllerAction).Should().HaveCount(1);

        IControllerActionClaim<byte> ca = claims.OfType<IControllerActionClaim<byte>>()
            .Single(c => c.ClaimType == (byte)ControllerAction);
        ca.ControllerId.Should().Be(controllerId);
        ca.ActionType.Should().Be(2);
    }

    [Fact]
    public async Task RefreshMiddleware_Skips_Malformed_ControllerAction_Claim()
    {
        Guid good = Guid.Parse("22222222-2222-2222-2222-222222222222");
        List<Claim> extra = new List<Claim>
        {
            new Claim(SdClaimTypes.ControllerAction, "not-a-valid-claim"),
            new Claim(SdClaimTypes.ControllerAction, $"{good}:1"),
            new Claim(SdClaimTypes.AccessRights, "not-a-byte")
        };
        var (context, tokenService, provider, captured) =
            BuildSetup(new SDConfigs { TokenRefreshThresholdMinutes = 10 },
                AuthorizedPrincipal(expClaimValue: FutureUnix(1), extraClaims: extra));

        await Invoke(context, provider);

        IClaim[]? claims = captured.Claims;
        claims.Should().NotBeNull();
        claims!.Where(c => c.ClaimType == (byte)ControllerAction).Should().HaveCount(1);
        claims.OfType<IControllerActionClaim<byte>>()
            .Single().ControllerId.Should().Be(good);
        claims.Where(c => c.ClaimType == (byte)AccessRight).Should().BeEmpty("non-numeric access-right value must be skipped");
    }

    [Fact]
    public async Task RefreshMiddleware_No_Uid_No_Refresh()
    {
        var (context, tokenService, provider, _) =
            BuildSetup(new SDConfigs { TokenRefreshThresholdMinutes = 10 },
                AuthorizedPrincipal(uid: null, expClaimValue: FutureUnix(1)));

        await Invoke(context, provider);

        context.Response.Headers.ContainsKey(ConstantKeys.AUTH_HEADER_KEY).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshMiddleware_Parses_Expiry_From_Unix_Seconds()
    {
        var near = BuildSetup(new SDConfigs { TokenRefreshThresholdMinutes = 10 },
            AuthorizedPrincipal(expClaimValue: FutureUnix(2), expClaimType: "exp"));
        var far = BuildSetup(new SDConfigs { TokenRefreshThresholdMinutes = 10 },
            AuthorizedPrincipal(expClaimValue: FutureUnix(120), expClaimType: "exp"));

        await Invoke(near.context, near.provider);
        await Invoke(far.context, far.provider);

        near.tokenService.Received(1).GenerateToken(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<IClaim[]>(), Arg.Any<bool>());
        far.tokenService.DidNotReceiveWithAnyArgs().GenerateToken(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<IClaim[]>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task RefreshMiddleware_Parses_Expiry_From_Date_String()
    {
        string nearDate = DateTime.UtcNow.AddMinutes(2).ToString("o", CultureInfo.InvariantCulture);
        string farDate = DateTime.UtcNow.AddMinutes(120).ToString("o", CultureInfo.InvariantCulture);

        var near = BuildSetup(new SDConfigs { TokenRefreshThresholdMinutes = 10 },
            AuthorizedPrincipal(expClaimValue: nearDate, expClaimType: "exp"));
        var far = BuildSetup(new SDConfigs { TokenRefreshThresholdMinutes = 10 },
            AuthorizedPrincipal(expClaimValue: farDate, expClaimType: "exp"));

        await Invoke(near.context, near.provider);
        await Invoke(far.context, far.provider);

        near.tokenService.Received(1).GenerateToken(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<IClaim[]>(), Arg.Any<bool>());
        far.tokenService.DidNotReceiveWithAnyArgs().GenerateToken(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<IClaim[]>(), Arg.Any<bool>());
    }

    private static string FutureUnix(int minutesFromNow)
    {
        long seconds = new DateTimeOffset(DateTime.UtcNow.AddMinutes(minutesFromNow)).ToUnixTimeSeconds();
        return seconds.ToString(CultureInfo.InvariantCulture);
    }

    private sealed class CapturedArgs
    {
        public string? Uid { get; set; }
        public byte[]? Roles { get; set; }
        public IClaim[]? Claims { get; set; }
        public bool MustChange { get; set; }
    }
}
