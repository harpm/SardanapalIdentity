using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Sardanapal.Identity.Authorization.Filters;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Xunit;

namespace Sardanapal.Identity.Authorization.Tests.Unit;

public class AuthorizationFiltersTests
{
    private static IIdentityProvider Provider(bool isAnonymous = false, bool isAuthorized = false, ClaimsPrincipal? claims = null)
    {
        IIdentityProvider provider = Substitute.For<IIdentityProvider>();
        provider.IsAnonymous.Returns(isAnonymous);
        provider.IsAuthorized.Returns(isAuthorized);
        provider.Claims.Returns(claims);
        return provider;
    }

    private sealed class NextCallTracker
    {
        public bool Called { get; set; }
    }

    private static (ActionExecutingContext context, ActionExecutionDelegate next, NextCallTracker tracker) BuildContext(
        IIdentityProvider provider)
    {
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(provider);
        DefaultHttpContext httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

        ActionContext actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        NextCallTracker tracker = new NextCallTracker();
        ActionExecutionDelegate next = () =>
        {
            tracker.Called = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller: null));
        };

        ActionExecutingContext executing = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: null);

        return (executing, next, tracker);
    }

    private static ClaimsPrincipal WithRole(byte role) => new ClaimsPrincipal(
        new ClaimsIdentity(new[] { new Claim(SdClaimTypes.Roles, role.ToString()) }, "Test"));

    private static ClaimsPrincipal WithAccessRight(byte id) => new ClaimsPrincipal(
        new ClaimsIdentity(new[] { new Claim(SdClaimTypes.AccessRights, id.ToString()) }, "Test"));

    private static ClaimsPrincipal WithMustChangePassword() => new ClaimsPrincipal(
        new ClaimsIdentity(new[] { new Claim(SdClaimTypes.MustChangePassword, "true") }, "Test"));

    [Fact]
    public async Task Anonymous_Filter_Runs_First_And_Allows_Unauthenticated()
    {
        IIdentityProvider provider = Provider();
        var (context, next, tracker) = BuildContext(provider);
        AnonymousAttribute filter = new AnonymousAttribute();

        filter.Order.Should().Be(0);
        await filter.OnActionExecutionAsync(context, next);

        provider.Received(1).SetAnonymous();
        tracker.Called.Should().BeTrue("Anonymous should not short-circuit the pipeline");
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task Authorize_No_Token_Returns_401()
    {
        IIdentityProvider provider = Provider(isAnonymous: false, isAuthorized: false);
        var (context, next, _) = BuildContext(provider);
        AuthorizeAttribute filter = new AuthorizeAttribute();

        filter.Order.Should().Be(1);
        await filter.OnActionExecutionAsync(context, next);

        context.HttpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        context.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Authorize_Token_With_MustChangePassword_Returns_403_With_Message()
    {
        IIdentityProvider provider = Provider(isAuthorized: true, claims: WithMustChangePassword());
        var (context, next, _) = BuildContext(provider);
        AuthorizeAttribute filter = new AuthorizeAttribute();

        await filter.OnActionExecutionAsync(context, next);

        context.HttpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
        context.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Authorize_Valid_Token_Passes()
    {
        ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(SdClaimTypes.NameIdentifier, "u1") }, "Test"));
        IIdentityProvider provider = Provider(isAuthorized: true, claims: principal);
        var (context, next, tracker) = BuildContext(provider);
        AuthorizeAttribute filter = new AuthorizeAttribute();

        await filter.OnActionExecutionAsync(context, next);

        context.HttpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        context.Result.Should().BeNull();
        tracker.Called.Should().BeTrue();
    }

    [Fact]
    public async Task HasRole_Missing_Role_Returns_401()
    {
        IIdentityProvider provider = Provider(isAuthorized: true, claims: WithRole(1));
        var (context, next, _) = BuildContext(provider);
        HasRoleAttribute filter = new HasRoleAttribute(2);

        filter.Order.Should().Be(3);
        await filter.OnActionExecutionAsync(context, next);

        context.HttpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        context.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task HasRole_Anonymous_Passes_Through()
    {
        IIdentityProvider provider = Provider(isAnonymous: true);
        var (context, next, tracker) = BuildContext(provider);
        HasRoleAttribute filter = new HasRoleAttribute(2);

        await filter.OnActionExecutionAsync(context, next);

        tracker.Called.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task HasRole_Missing_IdentityProvider_Returns_401()
    {
        ServiceCollection services = new ServiceCollection();
        DefaultHttpContext httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        ActionContext actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        ActionExecutingContext context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), null);
        NextCallTracker tracker = new NextCallTracker();
        ActionExecutionDelegate next = () => { tracker.Called = true; return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null)); };

        HasRoleAttribute filter = new HasRoleAttribute(1);

        await filter.OnActionExecutionAsync(context, next);

        context.HttpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        context.Result.Should().BeOfType<UnauthorizedResult>();
        tracker.Called.Should().BeFalse();
    }

    [Fact]
    public async Task HasAccessRight_Missing_Claim_Returns_401()
    {
        IIdentityProvider provider = Provider(isAuthorized: true, claims: WithAccessRight(1));
        var (context, next, _) = BuildContext(provider);
        HasAccessRightAttribute filter = new HasAccessRightAttribute(5);

        filter.Order.Should().Be(4);
        await filter.OnActionExecutionAsync(context, next);

        context.HttpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        context.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task HasAccessRight_Anonymous_Passes_Through()
    {
        IIdentityProvider provider = Provider(isAnonymous: true);
        var (context, next, tracker) = BuildContext(provider);
        HasAccessRightAttribute filter = new HasAccessRightAttribute(5);

        await filter.OnActionExecutionAsync(context, next);

        tracker.Called.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    [Fact]
    public void Filter_Execution_Order_Is_Anonymous_Authorize_HasRole_HasAccessRight()
    {
        new AnonymousAttribute().Order.Should().Be(0);
        new AuthorizeAttribute().Order.Should().Be(1);
        new HasRoleAttribute(1).Order.Should().Be(3);
        new HasAccessRightAttribute(1).Order.Should().Be(4);

        int[] ordered = new IActionFilter[]
        {
            new AuthorizeAttribute(),
            new HasAccessRightAttribute(1),
            new AnonymousAttribute(),
            new HasRoleAttribute(1)
        }.OrderBy(f => ((IOrderedFilter)f).Order).Select(f => ((IOrderedFilter)f).Order).ToArray();

        ordered.Should().BeInAscendingOrder();
    }
}
