using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Sardanapal.Identity.Authorization.Middlewares;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Xunit;

namespace Sardanapal.Identity.Authorization.Tests.Integration;

public class SdAuthorizationMiddlewareTests
{
    private static DefaultHttpContext ContextWithAuthHeader(string? headerName, string? token)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        if (headerName != null && token != null)
        {
            context.Request.Headers[headerName] = token;
        }
        return context;
    }

    private static SdAuthorizationMiddleware CreateMiddleware() =>
        new SdAuthorizationMiddleware(_ => Task.CompletedTask);

    [Theory]
    [InlineData("AUTH")]
    [InlineData("auth")]
    [InlineData("Auth")]
    [InlineData("aUtH")]
    public async Task Middleware_Reads_AUTH_Header_Case_Insensitive(string headerName)
    {
        DefaultHttpContext context = ContextWithAuthHeader(headerName, "my.token.value");
        IIdentityProvider provider = Substitute.For<IIdentityProvider>();
        SdAuthorizationMiddleware middleware = CreateMiddleware();

        await middleware.InvokeAsync(context, provider);

        provider.Received(1).SetAuthorize("my.token.value");
    }

    [Fact]
    public async Task Middleware_No_AUTH_Header_Does_Not_Call_SetAuthorize()
    {
        DefaultHttpContext context = new DefaultHttpContext();
        IIdentityProvider provider = Substitute.For<IIdentityProvider>();
        SdAuthorizationMiddleware middleware = CreateMiddleware();

        await middleware.InvokeAsync(context, provider);

        provider.DidNotReceiveWithAnyArgs().SetAuthorize(Arg.Any<string>());
    }

    [Fact]
    public async Task Middleware_Empty_TOKEN_Does_Not_Call_SetAuthorize()
    {
        DefaultHttpContext context = ContextWithAuthHeader(ConstantKeys.AUTH_HEADER_KEY, "   ");
        IIdentityProvider provider = Substitute.For<IIdentityProvider>();
        SdAuthorizationMiddleware middleware = CreateMiddleware();

        await middleware.InvokeAsync(context, provider);

        provider.DidNotReceiveWithAnyArgs().SetAuthorize(Arg.Any<string>());
    }

    [Fact]
    public async Task Middleware_Calls_Next_Delegate_After_Identity()
    {
        DefaultHttpContext context = new DefaultHttpContext();
        IIdentityProvider provider = Substitute.For<IIdentityProvider>();
        bool nextCalled = false;
        SdAuthorizationMiddleware middleware = new SdAuthorizationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, provider);

        nextCalled.Should().BeTrue();
    }
}
