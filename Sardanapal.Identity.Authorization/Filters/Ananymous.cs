
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Contract.IService;
using System.Net;

namespace Sardanapal.Identity.Authorization.Filters;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AnanymousAttribute : ActionFilterAttribute
{
    public AnanymousAttribute()
    {

    }

    public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            IIdentityProvider idProvider = context.HttpContext.RequestServices.GetRequiredService(typeof(IIdentityProvider)) as IIdentityProvider;
            idProvider.SetAnanymous();
        }
        catch (Exception ex)
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Result = new UnauthorizedResult();
        }
        
        return base.OnActionExecutionAsync(context, next);
    }
}
