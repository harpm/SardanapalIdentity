using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sardanapal.Identity.Authorization.Data;
using System.Net;
using System.Security.Claims;

namespace Sardanapal.Identity.Authorization.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class HasRoleAttribute : ActionFilterAttribute
{
    protected byte[] roleIds;
    public HasRoleAttribute(params byte[] _roleIds)
    {
        roleIds = _roleIds;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        try
        {
            IIdentityHolder idHolder = context.HttpContext.RequestServices.GetService(typeof(IIdentityHolder)) as IIdentityHolder;
            if (!idHolder.IsAuthorized
                || idHolder.Claims == null
                || idHolder.Claims.Claims == null
                || idHolder.Claims.Claims.Count() == 0
                || !idHolder.Claims.Claims
                    .Where(c => c.ValueType == ClaimTypes.Role
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
    }

    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        try
        {
            IIdentityHolder idHolder = context.HttpContext.RequestServices.GetService(typeof(IIdentityHolder)) as IIdentityHolder;
            if (!idHolder.IsAuthorized
                || idHolder.Claims == null
                || idHolder.Claims.Claims == null
                || idHolder.Claims.Claims.Count() == 0
                || !idHolder.Claims.Claims
                    .Where(c => c.ValueType == ClaimTypes.Role
                        && roleIds.Select(r => r.ToString()).Contains(c.Value)).Any())
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Result = new UnauthorizedResult();
            }
            else
            {
                await next();
            }
        }
        catch
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Result = new UnauthorizedResult();
        }
    }
}
