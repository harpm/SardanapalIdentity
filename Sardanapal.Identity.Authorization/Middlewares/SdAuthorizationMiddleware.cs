using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Sardanapal.Identity.Authorization.Data;
using Sardanapal.Identity.Services.Services;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Authorization.Middlewares;

public class SdAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    
    public SdAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public virtual async Task InvokeAsync(HttpContext context, ITokenService tokenService, IIdentityHolder identityHolder)
    {
        string token = context.Request.Headers
            .Where(x => x.Key.Equals("Auth", StringComparison.InvariantCultureIgnoreCase))
            .Select(x => x.Value)
            .FirstOrDefault();

        var res = tokenService.ValidateToken(token, out ClaimsPrincipal cp);
        if (res.StatusCode == StatusCode.Succeeded && res.Data)
        {
            identityHolder.SetAuthorize(token, cp, cp.FindFirst(ClaimTypes.NameIdentifier).Value);
        }

        // Call the next delegate/middleware in the pipeline.
        await _next(context);
    }
}