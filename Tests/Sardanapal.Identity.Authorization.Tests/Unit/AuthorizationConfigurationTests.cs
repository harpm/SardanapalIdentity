using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sardanapal.Identity.Authorization.Data;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.ViewModel.Response;
using Xunit;

namespace Sardanapal.Identity.Authorization.Tests.Unit;

internal sealed class FakeTokenService : ITokenService
{
    public string ServiceName => "FakeTokenService";

    public ClaimsPrincipal Principal { get; set; } = new ClaimsPrincipal(
        new ClaimsIdentity(new[]
        {
            new Claim(SdClaimTypes.NameIdentifier, "user-1"),
            new Claim(SdClaimTypes.Roles, "1")
        }, "Test"));

    public IResponse<bool> ValidateToken(string token, out ClaimsPrincipal claims)
    {
        claims = Principal;
        return new Response<bool>(NullLogger.Instance) { StatusCode = StatusCode.Succeeded, Data = true };
    }

    public IResponse<bool> ValidateTokenRoles(string token, byte[] roleIds, IClaim[] claims) =>
        new Response<bool>(NullLogger.Instance) { StatusCode = StatusCode.Succeeded, Data = true };

    public IResponse<string> GenerateToken(string uid, byte[] roleIds, IClaim[] claims, bool mustChangePassword = false) =>
        new Response<string>(NullLogger.Instance) { StatusCode = StatusCode.Succeeded, Data = "refreshed-token" };
}

public class AuthorizationConfigurationTests
{
    [Fact]
    public void DI_AddAuthServices_Registers_ITokenService_And_HttpContextAccessor()
    {
        ServiceCollection services = new ServiceCollection();

        services.AddAuthServices<FakeTokenService>();

        ServiceDescriptor? tokenDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenService));
        tokenDescriptor.Should().NotBeNull();
        tokenDescriptor!.ImplementationType.Should().Be<FakeTokenService>();

        services.Should().Contain(s => s.ServiceType == typeof(IHttpContextAccessor));
    }

    [Fact]
    public async Task DI_UseSardanapalAuthentication_Wires_Correct_Middleware_For_Flag()
    {
        FakeTokenService tokenService = new FakeTokenService();
        tokenService.Principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(SdClaimTypes.NameIdentifier, "user-1"),
            new Claim(SdClaimTypes.Roles, "1"),
            new Claim("exp", new DateTimeOffset(DateTime.UtcNow.AddMinutes(2)).ToUnixTimeSeconds().ToString())
        }, "Test"));

        SDConfigs refreshConfig = new SDConfigs { TokenRefreshThresholdMinutes = 10 };

        (ApplicationBuilder app, RequestDelegate pipeline, ServiceProvider provider) WithPipeline(bool withRefresh)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddAuthServices<FakeTokenService>();
            services.AddScoped(_ => tokenService);
            services.AddScoped<IIdentityProvider, IdentityProvider>();
            services.AddSingleton<IOptions<SDConfigs>>(Options.Create(refreshConfig));
            ServiceProvider sp = services.BuildServiceProvider();

            ApplicationBuilder builder = new ApplicationBuilder(sp);
            builder.UseSardanapalAuthentication(withRefresh);
            builder.Run(ctx =>
            {
                ctx.Response.StatusCode = 200;
                return Task.CompletedTask;
            });
            return (builder, builder.Build(), sp);
        }

        static DefaultHttpContext BuildContext(ServiceProvider provider)
        {
            DefaultHttpContext context = new DefaultHttpContext { RequestServices = provider };
            context.Request.Headers[ConstantKeys.AUTH_HEADER_KEY] = "near-expiry.token";
            context.Response.Headers.Clear();
            return context;
        }

        var (_, refreshPipeline, refreshProvider) = WithPipeline(true);
        DefaultHttpContext refreshContext = BuildContext(refreshProvider);
        await refreshPipeline(refreshContext);
        refreshContext.Response.Headers.ContainsKey(ConstantKeys.AUTH_HEADER_KEY)
            .Should().BeTrue("the refresh middleware should reissue the token");

        var (_, plainPipeline, plainProvider) = WithPipeline(false);
        DefaultHttpContext plainContext = BuildContext(plainProvider);
        await plainPipeline(plainContext);
        plainContext.Response.Headers.ContainsKey(ConstantKeys.AUTH_HEADER_KEY)
            .Should().BeFalse("the plain middleware must not refresh");
    }
}
