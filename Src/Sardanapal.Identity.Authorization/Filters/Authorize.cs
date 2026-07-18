
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;

namespace Sardanapal.Identity.Authorization.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AuthorizeAttribute : ActionFilterAttribute
{
    public AuthorizeAttribute()
    {
        Order = 1;
    }

    public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context != null)
        {
            IIdentityProvider idHolder = context.HttpContext.RequestServices.GetRequiredService(typeof(IIdentityProvider)) as IIdentityProvider;

            if (!idHolder.IsAnonymous && !idHolder.IsAuthorized)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Result = new UnauthorizedResult();
                return Task.CompletedTask;
            }

            if (idHolder.IsAuthorized && idHolder.Claims != null
                && idHolder.Claims.HasClaim(SdClaimTypes.MustChangePassword, "true"))
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Result = new ObjectResult(new { message = "Password change required." })
                {
                    StatusCode = (int)HttpStatusCode.Forbidden
                };
                return Task.CompletedTask;
            }
        }

        return base.OnActionExecutionAsync(context, next);
    }
}
