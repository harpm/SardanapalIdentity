using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sardanapal.Identity.Authorization.Data;
using Sardanapal.Identity.Services.Services;

namespace Sardanapal.Identity.Authorization.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AuthorizeAttribute : ActionFilterAttribute
{
    protected ITokenService _tokenService;
    public AuthorizeAttribute(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        try
        {
            IIdentityHolder idHolder = context.HttpContext.RequestServices.GetService(typeof(IIdentityHolder)) as IIdentityHolder;
            if (!idHolder.IsAuthorized)
            {
                context.Result = new UnauthorizedResult();
            }
        }
        catch (Exception ex)
        {
            context.Result = new UnauthorizedResult();
        }
    }

    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        try
        {
            IIdentityHolder idHolder = context.HttpContext.RequestServices.GetService(typeof(IIdentityHolder)) as IIdentityHolder;
            if (!idHolder.IsAuthorized)
            {
                context.Result = new UnauthorizedResult();
            }
            else
            {
                await next();
            }

        }
        catch (Exception ex)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}