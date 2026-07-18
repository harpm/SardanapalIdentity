
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Contract.IService;

namespace Sardanapal.Identity.Authorization.Filters;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class AnonymousAttribute : ActionFilterAttribute
{
    public AnonymousAttribute()
    {
        this.Order = 0;
    }

    public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        IIdentityProvider? idProvider = context?.HttpContext?.RequestServices?.GetRequiredService(typeof(IIdentityProvider)) as IIdentityProvider;
        idProvider?.SetAnonymous();

        return base.OnActionExecutionAsync(context, next);
    }
}
