using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
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

    protected virtual string GenerateToken(string uid, int expireTime, byte[] roleIds, byte[] claimIds)
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

        var Credentials = new SigningCredentials(_configs.TokenParameters.IssuerSigningKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_configs.TokenParameters.ValidIssuer
            , _configs.TokenParameters.ValidAudience
            , Claims
            , expires: DateTime.UtcNow.AddMinutes(expireTime)
            , signingCredentials: Credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public virtual IResponse<string> GenerateToken(string uid, byte[] roleIds, byte[] claimIds)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Function, _logger);

        return result.Fill(() =>
        {
            string token = GenerateToken(uid, _configs.ExpirationTime, roleIds, []);

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

    public virtual IResponse<bool> ValidateTokenRoles(string token, byte[] roleIds, byte[] claimIds)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Fetch, _logger);

        return result.Fill(() =>
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var claimsPrinc = tokenHandler
                .ValidateToken(token, _configs.TokenParameters, out SecurityToken validatedToken);

            var hasRole = claimsPrinc.HasClaim(c => c.Type == SdClaimTypes.Roles
                && roleIds.Select(r => r.ToString()).Contains(c.Value));

            if (hasRole)
            {
                result.Set(StatusCode.Succeeded, hasRole);
            }
            else
            {
                result.Set(StatusCode.NotExists, false);
            }

        });
    }
}
