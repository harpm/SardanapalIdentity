using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Sardanapal.Identity.Authorization.Filters;
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

    protected virtual Task ProcessIdentity(HttpContext context, ITokenService tokenService, IIdentityProvider identityProvider)
    {
        if (context == null) return Task.CompletedTask;

        string token = context?.Request?.Headers?
            .Where(x => x.Key.Equals(ConstantKeys.AUTH_HEADER_KEY, StringComparison.InvariantCultureIgnoreCase))
            .Select(x => x.Value)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(token))
        {
            var res = tokenService.ValidateToken(token, out ClaimsPrincipal cp);
            if (res.StatusCode == StatusCode.Succeeded && res.Data)
            {
                identityProvider.SetAuthorize(token, cp, cp?.FindFirst(SdClaimTypes.NameIdentifier)?.Value);
            }

            var descriptor = context.GetEndpoint()?.Metadata.GetMetadata<ActionDescriptor>();

            if (descriptor?.FilterDescriptors
                .Where(f => f.GetType() == typeof(AnanymousAttribute))
                .Any() ?? false)
            {
                identityProvider.SetAnanymous();
            }
        }

        return Task.CompletedTask;
    }

    public virtual async Task InvokeAsync(HttpContext context, ITokenService tokenService, IIdentityProvider identityProvider)
    {
        await ProcessIdentity(context, tokenService, identityProvider);

        // Call the next delegate/middleware in the pipeline.
        await _next(context);
    }
}
