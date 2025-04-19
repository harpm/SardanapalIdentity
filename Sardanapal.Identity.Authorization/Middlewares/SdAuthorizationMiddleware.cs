using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Authorization.Middlewares;

public class SdAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    
    public SdAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public virtual async Task InvokeAsync(HttpContext context, ITokenService tokenService, IIdentityProvider identityProvider)
    {
        string token = context.Request.Headers
            .Where(x => x.Key.Equals(ConstantKeys.AUTH_HEADER_KEY, StringComparison.InvariantCultureIgnoreCase))
            .Select(x => x.Value)
            .FirstOrDefault();

        var res = tokenService.ValidateToken(token, out ClaimsPrincipal cp);
        if (res.StatusCode == StatusCode.Succeeded && res.Data)
        {
            identityProvider.SetAuthorize(token, cp, cp.FindFirst(ClaimTypes.NameIdentifier).Value);
        }

        // Call the next delegate/middleware in the pipeline.
        await _next(context);
    }
}