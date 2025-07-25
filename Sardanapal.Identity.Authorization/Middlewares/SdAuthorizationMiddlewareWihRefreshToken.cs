
using Microsoft.AspNetCore.Http;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Authorization.Middlewares;

public class SdAuthorizationMiddlewareWihRefreshToken : SdAuthorizationMiddleware
{
    public SdAuthorizationMiddlewareWihRefreshToken(RequestDelegate next) : base(next)
    {

    }

    protected override async Task ProcessIdentity(HttpContext context, ITokenService tokenService, IIdentityProvider identityProvider)
    {
        await base.ProcessIdentity(context, tokenService, identityProvider);
        
        if (identityProvider.IsAuthorized)
        {
            var token = tokenService.GenerateToken(identityProvider.Claims.FindFirst(SdClaimTypes.NameIdentifier).Value
                        , identityProvider.Claims.FindAll(SdClaimTypes.Roles).Select(c => Convert.ToByte(c.Value))
                        .ToArray(), []);

            if (token.StatusCode == StatusCode.Succeeded)
            {
                context.Response.Headers[ConstantKeys.AUTH_HEADER_KEY] = token.StatusCode.ToString();
            }
        }
    }
}
