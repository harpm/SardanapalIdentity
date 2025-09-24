using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services;

public class TokenService : ITokenService
{
    protected readonly ILogger _logger;
    public string ServiceName => "TokenService";

    public TokenService(ILogger logger)
    {

    }

    protected virtual string GenerateToken(string uid, int expireTime, byte[] roleIds, byte[] claimIds)
    {
        if (StaticConfigs.TokenParameters == null) throw new NullReferenceException(nameof(StaticConfigs.TokenParameters));

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

        var Credentials = new SigningCredentials(StaticConfigs.TokenParameters.IssuerSigningKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(StaticConfigs.TokenParameters.ValidIssuer
            , StaticConfigs.TokenParameters.ValidAudience
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
            string token = GenerateToken(uid, StaticConfigs.ExpirationTime, roleIds, []);

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
            claims = tokenHandler.ValidateToken(token, StaticConfigs.TokenParameters, out SecurityToken validatedToken);

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
                .ValidateToken(token, StaticConfigs.TokenParameters, out SecurityToken validatedToken);

            result.Set(StatusCode.Succeeded, claimsPrinc.HasClaim(c => c.Type == SdClaimTypes.Roles
                && roleIds.Select(r => r.ToString()).Contains(c.Value)));
        });
    }
}
