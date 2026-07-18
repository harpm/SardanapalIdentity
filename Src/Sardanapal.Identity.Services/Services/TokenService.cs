using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.Identity.Share.Types;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services;

public class TokenService : ITokenService
{
    protected readonly SDConfigs _configs;
    protected readonly ILogger _logger;
    public string ServiceName => "TokenService";

    public TokenService(IOptions<SDConfigs> config, ILogger logger)
    {
        if (config?.Value == null) throw new ArgumentNullException(nameof(SDConfigs));
        this._logger = logger;
        this._configs = config.Value;
    }

    protected virtual string GenerateToken(string uid, int expireTime, byte[] roleIds, IClaim[] claims, bool mustChangePassword = false)
    {
        if (_configs.TokenParameters == null) throw new NullReferenceException(nameof(SDConfigs.TokenParameters));

        var roleClaims = new Claim[roleIds.Length];
        for (int i = 0; i < roleIds.Length; i++)
        {
            roleClaims[i] = new Claim(SdClaimTypes.Roles, roleIds[i].ToString());
        }

        var Claims = new List<Claim>()
            {
                new Claim(SdClaimTypes.NameIdentifier, uid.ToString())
            };

        Claims.AddRange(roleClaims);
        Claims.AddRange(MapTokenClaims(claims));

        if (mustChangePassword)
        {
            Claims.Add(new Claim(SdClaimTypes.MustChangePassword, "true"));
        }

        var Credentials = new SigningCredentials(_configs.TokenParameters.IssuerSigningKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_configs.TokenParameters.ValidIssuer
            , _configs.TokenParameters.ValidAudience
            , Claims
            , expires: DateTime.UtcNow.AddMinutes(expireTime)
            , signingCredentials: Credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected virtual IEnumerable<Claim> MapTokenClaims(IClaim[] claims)
    {
        if (claims == null)
            yield break;

        foreach (var claim in claims)
        {
            if (claim == null)
                continue;

            switch ((SdClaimType)claim.ClaimType)
            {
                case SdClaimType.ControllerAction when claim is IControllerActionClaim<byte> ca:
                    yield return new Claim(SdClaimTypes.ControllerAction,
                        $"{ca.ControllerId}:{ca.ActionType}");
                    break;
                case SdClaimType.AccessRight when claim is IClaim<byte> ac:
                    yield return new Claim(SdClaimTypes.AccessRights, ac.Id.ToString());
                    break;
            }
        }
    }

    public virtual IResponse<string> GenerateToken(string uid, byte[] roleIds, IClaim[] claims, bool mustChangePassword = false)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Function, _logger);

        return result.Fill(() =>
        {
            string token = GenerateToken(uid, _configs.ExpirationTime, roleIds, claims ?? [], mustChangePassword);

            result.Set(StatusCode.Succeeded, token);
        });
    }

    public virtual IResponse<bool> ValidateToken(string token, out ClaimsPrincipal claims)
    {
        IResponse<bool> result = new Response<bool>(ServiceName, OperationType.Fetch, _logger);
        claims = new ClaimsPrincipal();

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            claims = tokenHandler.ValidateToken(token, _configs.TokenParameters, out SecurityToken validatedToken);

            result.Set(StatusCode.Succeeded, true);
            return result;
        }
        catch (Exception ex)
        {
            result.Set(StatusCode.Exception, ex);
        }

        return result;
    }

    public virtual IResponse<bool> ValidateTokenRoles(string token, byte[] roleIds, IClaim[] claims)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Fetch, _logger);

        return result.Fill(() =>
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var claimsPrinc = tokenHandler
                .ValidateToken(token, _configs.TokenParameters, out SecurityToken validatedToken);

            var hasRole = claimsPrinc.HasClaim(c => c.Type == SdClaimTypes.Roles
                && roleIds.Select(r => r.ToString()).Contains(c.Value));

            bool hasClaim = HasClaims(claimsPrinc, claims);

            if (hasRole && hasClaim)
            {
                result.Set(StatusCode.Succeeded, true);
            }
            else
            {
                result.Set(StatusCode.NotExists, false);
            }

        });
    }

    protected virtual bool HasClaims(ClaimsPrincipal claimsPrincipal, IClaim[] claims)
    {
        if (claims == null || claims.Length == 0)
            return true;

        return claims.Any(claim =>
        {
            if (claim == null) return false;

            switch ((SdClaimType)claim.ClaimType)
            {
                case SdClaimType.ControllerAction when claim is IControllerActionClaim<byte> ca:
                    return claimsPrincipal.HasClaim(SdClaimTypes.ControllerAction,
                        $"{ca.ControllerId}:{ca.ActionType}");
                case SdClaimType.AccessRight when claim is IClaim<byte> ac:
                    return claimsPrincipal.HasClaim(SdClaimTypes.AccessRights, ac.Id.ToString());
                default:
                    return false;
            }
        });
    }
}
