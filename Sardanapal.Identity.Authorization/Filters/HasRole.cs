using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sardanapal.Identity.Services.Services;

namespace Sardanapal.Identity.Authorization.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class HasRoleAttribute : ActionFilterAttribute
{
    protected byte[] roleIds;
    public HasRoleAttribute(byte[] _roleIds)
    {
        roleIds = _roleIds;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        try
        {
            ITokenService? tokenService = context?.HttpContext?.RequestServices?.GetService(typeof(ITokenService)) as ITokenService;

            string? token = context?.HttpContext.Request.Headers
                .Where(h => h.Key == "Auth")
                .Select(h => h.Value)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(token))
            {
                context.Result = new UnauthorizedResult();
            }
            else
            {
                if (!roleIds.Any(r => tokenService.ValidateTokenRole(token, r)))
                {
                    context.Result = new UnauthorizedResult();
                }
            }
        }
        catch
        {
            context.Result = new UnauthorizedResult();
        }
    }

    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        try
        {
            ITokenService? tokenService = context?.HttpContext?.RequestServices?.GetService(typeof(ITokenService)) as ITokenService;

            string? token = context?.HttpContext.Request.Headers
                .Where(h => h.Key == "Auth")
                .Select(h => h.Value)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(token))
            {
                context.Result = new UnauthorizedResult();
            }
            else
            {
                if (!roleIds.Any(r => tokenService.ValidateTokenRole(token, r)))
                {
                    context.Result = new UnauthorizedResult();
                }
                else
                {
                    await next();
                }
            }
        }
        catch
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
