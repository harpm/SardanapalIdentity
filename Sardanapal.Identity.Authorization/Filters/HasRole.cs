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
        roleIds = _roleIds;
    }

    public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            IIdentityProvider idProvider = context?.HttpContext?.RequestServices?.GetRequiredService(typeof(IIdentityProvider)) as IIdentityProvider;

            if (idProvider?.IsAnanymous ?? false)
                return base.OnActionExecutionAsync(context, next);

            if (!idProvider.IsAuthorized
                || idProvider.Claims == null
                || idProvider.Claims.Claims == null
                || idProvider.Claims.Claims.Count() == 0
                || !idProvider.Claims.Claims
                    .Where(c => c.Type == SdClaimTypes.Role
                        && roleIds.Select(r => r.ToString()).Contains(c.Value)).Any())
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Result = new UnauthorizedResult();
            }
        }
        catch
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Result = new UnauthorizedResult();
        }

        return base.OnActionExecutionAsync(context, next);
    }
}
