
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Contract.IService;

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

            if (!idHolder.IsAnanymous && !idHolder.IsAuthorized)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Result = new UnauthorizedResult();
                return Task.CompletedTask;
            }
        }

        return base.OnActionExecutionAsync(context, next);
    }
}
