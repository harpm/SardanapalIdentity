using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;

namespace Sardanapal.Identity.Authorization.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class HasAccessRightAttribute : ActionFilterAttribute
{
    protected byte[] accessRightIds;
    public HasAccessRightAttribute(params byte[] _accessRightIds)
    {
        Order = 4;
        accessRightIds = _accessRightIds;
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

        if (idProvider.IsAnonymous)
            return base.OnActionExecutionAsync(context, next);

        if (!idProvider.IsAuthorized
            || idProvider.Claims == null
            || !idProvider.Claims.HasClaim(c => c.Type == SdClaimTypes.AccessRights
                && accessRightIds.Select(cl => cl.ToString()).Contains(c.Value)))
        {
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Result = new UnauthorizedResult();
            return Task.CompletedTask;
        }

        return base.OnActionExecutionAsync(context, next);
    }
}
