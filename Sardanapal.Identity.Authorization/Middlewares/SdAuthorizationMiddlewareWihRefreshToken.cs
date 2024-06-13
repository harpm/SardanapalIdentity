
using Microsoft.AspNetCore.Http;
using Sardanapal.Identity.Authorization.Data;
using Sardanapal.Identity.Services.Services;
using Sardanapal.ViewModel.Response;
using System.Security.Claims;

namespace Sardanapal.Identity.Authorization.Middlewares;

public class SdAuthorizationMiddlewareWihRefreshToken : SdAuthorizationMiddleware
{
    public SdAuthorizationMiddlewareWihRefreshToken(RequestDelegate next) : base(next)
    {

    }

    public override Task InvokeAsync(HttpContext context, ITokenService tokenService, IIdentityHolder identityHolder)
    {
        var result = base.InvokeAsync(context, tokenService, identityHolder);

        var token = tokenService.GenerateToken(identityHolder.Claims.FindFirst(ClaimTypes.NameIdentifier).Value
                    , identityHolder.Claims.FindAll(ClaimTypes.Role).Select(c => Convert.ToByte(c.Value))
                    .ToArray());

        if (token.StatusCode == StatusCode.Succeeded)
        {
            context.Response.Headers["Auth"] = token.StatusCode.ToString();
        }

        return result;
    }
}
