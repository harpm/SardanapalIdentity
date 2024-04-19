using Microsoft.AspNetCore.Http;
using Sardanapal.Identity.Authorization.Data;
using Sardanapal.Identity.Services.Services;
using Sardanapal.ViewModel.Response;
using System.Security.Claims;

namespace Sardanapal.Identity.Authorization.Middlewares;

public class SdAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITokenService _tokenService;
    private readonly IIdentityHolder _identityHolder;

    public SdAuthorizationMiddleware(RequestDelegate next, ITokenService tokenService, IIdentityHolder identityHolder)
    {
        _next = next;
        _tokenService = tokenService;
        _identityHolder = identityHolder;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string token = context.Request.Headers
            .Where(x => x.Key.Equals("Auth", StringComparison.InvariantCultureIgnoreCase))
            .Select(x => x.Value)
            .FirstOrDefault();

        var res = _tokenService.ValidateToken(token, out ClaimsPrincipal cp);
        if (res.StatusCode == StatusCode.Succeeded && res.Data)
        {
            _identityHolder.SetUserId(cp.FindFirst(ClaimTypes.NameIdentifier).Value);
            _identityHolder.SetToken(token);
            _identityHolder.SetAuthorize();
        }

        // Call the next delegate/middleware in the pipeline.
        await _next(context);
    }
}
