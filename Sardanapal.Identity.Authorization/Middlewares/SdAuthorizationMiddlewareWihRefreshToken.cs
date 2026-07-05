
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Authorization.Middlewares;

public class SdAuthorizationMiddlewareWihRefreshToken : SdAuthorizationMiddleware
{
    public SdAuthorizationMiddlewareWihRefreshToken(RequestDelegate next) : base(next)
    {

    }

    protected override async Task ProcessIdentity(HttpContext context, IIdentityProvider identityProvider)
    {
        await base.ProcessIdentity(context, identityProvider);

        if (!identityProvider.IsAuthorized || identityProvider.Claims == null)
            return;

        var configs = context.RequestServices.GetService<IOptions<SDConfigs>>()?.Value;
        int threshold = configs?.TokenRefreshThresholdMinutes ?? 0;

        // A threshold <= 0 disables automatic refresh.
        if (threshold <= 0)
            return;

        DateTime? expiry = TryGetTokenExpiry(identityProvider.Claims);
        if (expiry != null && (expiry.Value - DateTime.UtcNow).TotalMinutes > threshold)
            return;

        var tokenService = context.RequestServices.GetRequiredService<ITokenService>();

        var uid = identityProvider.Claims.FindFirst(SdClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(uid))
            return;

        var roles = identityProvider.Claims.FindAll(SdClaimTypes.Roles)
            .Select(c => Convert.ToByte(c.Value))
            .ToArray();

        bool mustChangePassword = identityProvider.Claims.HasClaim(SdClaimTypes.MustChangePassword, "true");

        var token = tokenService.GenerateToken(uid, roles, [], mustChangePassword);

        if (token.StatusCode == StatusCode.Succeeded)
        {
            context.Response.Headers[ConstantKeys.AUTH_HEADER_KEY] = token.Data;
        }
    }

    private static DateTime? TryGetTokenExpiry(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.Expiration) ?? principal.FindFirst("exp");
        if (claim == null || string.IsNullOrWhiteSpace(claim.Value))
            return null;

        if (long.TryParse(claim.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long seconds))
            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;

        if (DateTime.TryParse(claim.Value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            return dt;

        return null;
    }
}
