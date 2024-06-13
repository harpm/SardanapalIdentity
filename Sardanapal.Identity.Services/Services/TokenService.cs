using Microsoft.IdentityModel.Tokens;
using Sardanapal.Identity.Share.Static;
using Sardanapal.ViewModel.Response;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Sardanapal.Identity.Services.Services;

public interface ITokenService
{
    IResponse<bool> ValidateToken(string token, out ClaimsPrincipal claims);
    IResponse<bool> ValidateTokenRole(string token, byte roleId);
    IResponse<bool> ValidateTokenRoles(string token, byte[] roleIds);
    IResponse<string> GenerateToken<TUserKey>(TUserKey uid, byte roleId);
    IResponse<string> GenerateToken<TUserKey>(TUserKey uid, byte[] roleId);
}

public class TokenService : ITokenService
{
    public string ServiceName => "TokenService";

    public TokenService()
    {

    }

    protected virtual string GenerateToken<TUserKey>(TUserKey uid, int expireTime, params byte[] roleIds)
    {
        var roleClaims = new Claim[roleIds.Length];
        for (int i = 0; i < roleIds.Length; i++)
        {
            roleClaims[i] = new Claim(ClaimTypes.Role, roleIds[0].ToString());
        }

        var Claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, uid.ToString())
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

    public virtual IResponse<string> GenerateToken<TUserKey>(TUserKey uid, byte roleId)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Function);

        return result.Fill(() =>
        {
            string token = GenerateToken(uid, StaticConfigs.ExpirationTime, roleId);
            result.Set(StatusCode.Succeeded, token);
        });
    }

    public virtual IResponse<string> GenerateToken<TUserKey>(TUserKey uid, params byte[] roleIds)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Function);

        return result.Fill(() =>
        {
            string token = GenerateToken(uid, StaticConfigs.ExpirationTime, roleIds);

            result.Set(StatusCode.Succeeded, token);
        });
    }

    public virtual IResponse<bool> ValidateToken(string token, out ClaimsPrincipal claims)
    {
        IResponse<bool> result = new Response<bool>(ServiceName, OperationType.Function);
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

    public virtual IResponse<bool> ValidateTokenRoles(string token, byte[] roleIds)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Function);

        return result.Fill(() =>
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var claimsPrinc = tokenHandler
                .ValidateToken(token, StaticConfigs.TokenParameters, out SecurityToken validatedToken);

            result.Set(StatusCode.Succeeded, claimsPrinc.HasClaim(c => c.Type == ClaimTypes.Role
                && roleIds.Select(r => r.ToString()).Contains(c.Value)));
        });
    }

    public virtual IResponse<bool> ValidateTokenRole(string token, byte roleId)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Function);

        return result.Fill(() =>
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var claimsPrinc = tokenHandler
                .ValidateToken(token, StaticConfigs.TokenParameters, out SecurityToken validatedToken);

            result.Set(StatusCode.Succeeded
                , claimsPrinc.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == roleId.ToString()));
        });
    }
}