using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;

namespace Sardanapal.Identity.Authorization.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class HasRoleAttribute : ActionFilterAttribute
{
    protected byte[] roleIds;
    public HasRoleAttribute(params byte[] _roleIds)
    {
        Order = 3;
        roleIds = _roleIds;
    }

    public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        IIdentityProvider idProvider;
        try
        {
            idProvider = context?.HttpContext?.RequestServices?.GetRequiredService(typeof(IIdentityProvider)) as IIdentityProvider;
        }
        catch (InvalidOperationException)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        if (idProvider == null)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        if (idProvider.IsAnanymous)
            return base.OnActionExecutionAsync(context, next);

        if (!idProvider.IsAuthorized
            || idProvider.Claims == null
            || !idProvider.Claims.HasClaim(c => c.Type == SdClaimTypes.Roles
                && roleIds.Select(r => r.ToString()).Contains(c.Value)))
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        return base.OnActionExecutionAsync(context, next);
    }
}
