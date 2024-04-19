using Microsoft.IdentityModel.Tokens;
using Sardanapal.Identity.Share.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sardanapal.Identity.Services.Services;

public interface ITokenService
{
    IdentityInfo Info { get; set; }

    bool ValidateToken(string token);
    public bool ValidateTokenRole(string token, byte roleId);
    public bool ValidateTokenRoles(string token, byte[] roleIds);
    string GenerateToken<TUserKey>(TUserKey uid, byte roleId);
}
public class TokenService : ITokenService
{
    public IdentityInfo Info { get; set; }

    public string GenerateToken<TUserKey>(TUserKey uid, byte roleId)
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
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // TODO: Needs review
    public bool ValidateToken(string token)
    {
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
            tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            return true;
        }
        catch
        {
            // Log the reason why the token is not valid
            return false;
        }
    }

    public bool ValidateTokenRoles(string token, byte[] roleIds)
    {
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
            var claimsPrinc = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            if (claimsPrinc.HasClaim(c => c.Type == ClaimTypes.Role
                && roleIds.Select(r => r.ToString()).Contains(c.Value)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (SecurityTokenValidationException ex)
        {
            // Log the reason why the token is not valid
            return false;
        }
    }

    public bool ValidateTokenRole(string token, byte roleId)
    {
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
            var claimsPrinc = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            if (claimsPrinc.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == roleId.ToString()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (SecurityTokenValidationException ex)
        {
            // Log the reason why the token is not valid
            return false;
        }
    }
}