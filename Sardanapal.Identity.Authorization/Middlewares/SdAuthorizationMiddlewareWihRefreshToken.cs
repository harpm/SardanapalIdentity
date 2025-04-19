
using Microsoft.AspNetCore.Http;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.ViewModel.Response;
using System.Security.Claims;

namespace Sardanapal.Identity.Authorization.Middlewares;

public class SdAuthorizationMiddlewareWihRefreshToken : SdAuthorizationMiddleware
{
    public SdAuthorizationMiddlewareWihRefreshToken(RequestDelegate next) : base(next)
    {

    }

    public override async Task InvokeAsync(HttpContext context, ITokenService tokenService, IIdentityProvider identityProvider)
    {
        await base.InvokeAsync(context, tokenService, identityProvider);

        if (identityProvider.IsAuthorized)
        {
            var token = tokenService.GenerateToken(identityProvider.Claims.FindFirst(ClaimTypes.NameIdentifier).Value
                        , identityProvider.Claims.FindAll(ClaimTypes.Role).Select(c => Convert.ToByte(c.Value))
                        .ToArray());

            if (token.StatusCode == StatusCode.Succeeded)
            {
                context.Response.Headers[ConstantKeys.AUTH_HEADER_KEY] = token.StatusCode.ToString();
            }
        }
    }
}
