using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Sardanapal.Identity.Share.Options;

public class IdentityInfo
{
    public string? ConnectionString { get; set; }
    public string? RedisConnectionString { get; set; }
    public TokenValidationParameters TokenParameters { get; set; }
    public int ExpirationTime { get; set; }
    public int? OTPLength { get; set; }

    public IdentityInfo(IConfiguration config)
    {
        ConnectionString = config.GetConnectionString("Main");
        RedisConnectionString = config.GetConnectionString("Redis");
        var TokenProvider = config.GetSection("TokenProvider");
        string secretKeyStr = TokenProvider.GetSection("SecretKey").Value ?? "";
        var SymmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKeyStr));
        TokenParameters = new TokenValidationParameters()
        {
            ValidIssuer = TokenProvider.GetSection("Issuer").Value,
            ValidAudience = TokenProvider.GetSection("Audience").Value,
            IssuerSigningKey = SymmetricKey
        };
        ExpirationTime = Convert.ToInt32(TokenProvider.GetSection("TokenExpireTime").Value);
        OTPLength = Convert.ToInt32(TokenProvider.GetSection("OtpLength").Value);
    }
}

