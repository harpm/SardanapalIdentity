using Microsoft.IdentityModel.Tokens;
using Sardanapal.Identity.Share.Options;
using Sardanapal.ViewModel.Response;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sardanapal.Identity.Services.Services;

public interface ITokenService
{
    IResponse<bool> ValidateToken(string token, out ClaimsPrincipal claims);
    IResponse<bool> ValidateTokenRole(string token, byte roleId);
    IResponse<bool> ValidateTokenRoles(string token, byte[] roleIds);
    IResponse<string> GenerateToken<TUserKey>(TUserKey uid, byte roleId);
}
public class TokenService : ITokenService
{
    public string ServiceName => "TokenService";

    public readonly IdentityInfo Info;

    public TokenService(IdentityInfo identityInfo)
    {
        this.Info = identityInfo;
    }

    public IResponse<string> GenerateToken<TUserKey>(TUserKey uid, byte roleId)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Function);

        return result.Fill(() =>
        {
            var SymmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Info.SecretKey));
            var Credentials = new SigningCredentials(SymmetricKey, SecurityAlgorithms.HmacSha256);
            var Claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, uid.ToString()),
                new Claim(ClaimTypes.Role, roleId.ToString())
            };
            var token = new JwtSecurityToken(Info.Issuer, Info.Audience, Claims
                , expires: DateTime.UtcNow.AddMinutes(Info.ExpirationTime)
                , signingCredentials: Credentials);
            result.Set(StatusCode.Succeeded, new JwtSecurityTokenHandler().WriteToken(token));
            return result;
        });
    }

    // TODO: Needs review
    public IResponse<bool> ValidateToken(string token, out ClaimsPrincipal claims)
    {
        IResponse<bool> result = new Response<bool>(ServiceName, OperationType.Function);
        claims = new ClaimsPrincipal();
        
        try
        {
            var SymmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Info.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Info.Issuer,
                ValidateAudience = true,
                ValidAudience = Info.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SymmetricKey,
                ValidateLifetime = true
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            claims = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            result.Set(StatusCode.Succeeded, true);
            return result;
        }
        catch (Exception ex)
        {
            result.Set(StatusCode.Exception, ex);
        }

        return result;
    }

    public IResponse<bool> ValidateTokenRoles(string token, byte[] roleIds)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Function);

        return result.Fill(() => 
        {
            var SymmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Info.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Info.Issuer,
                ValidateAudience = true,
                ValidAudience = Info.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SymmetricKey,
                ValidateLifetime = true
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var claimsPrinc = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            result.Set(StatusCode.Succeeded, claimsPrinc.HasClaim(c => c.Type == ClaimTypes.Role
                && roleIds.Select(r => r.ToString()).Contains(c.Value)));
            
            return result;
        });
    }

    public IResponse<bool> ValidateTokenRole(string token, byte roleId)
    {
        IResponse<bool> result = new Response(ServiceName, OperationType.Function);

        return result.Fill(() =>
        {
            var SymmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Info.SecretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Info.Issuer,
                ValidateAudience = true,
                ValidAudience = Info.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = SymmetricKey,
                ValidateLifetime = true
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var claimsPrinc = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            result.Set(StatusCode.Succeeded, claimsPrinc.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == roleId.ToString()));
            return result;
        });
    }
}