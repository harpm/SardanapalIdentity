﻿
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

    public override async Task InvokeAsync(HttpContext context, ITokenService tokenService, IIdentityHolder identityHolder)
    {
        await base.InvokeAsync(context, tokenService, identityHolder);

        if (identityHolder.IsAuthorized)
        {
            var token = tokenService.GenerateToken(identityHolder.Claims.FindFirst(ClaimTypes.NameIdentifier).Value
                        , identityHolder.Claims.FindAll(ClaimTypes.Role).Select(c => Convert.ToByte(c.Value))
                        .ToArray());

            if (token.StatusCode == StatusCode.Succeeded)
            {
                context.Response.Headers["Auth"] = token.StatusCode.ToString();
            }
        }
    }
}
