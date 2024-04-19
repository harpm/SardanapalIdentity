using Microsoft.Extensions.Configuration;

namespace Sardanapal.Identity.Share.Options;

public class IdentityInfo
{
    public string? ConnectionString { get; set; }
    public string? RedisConnectionString { get; set; }
    public string? SecretKey { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public int ExpirationTime { get; set; }
    public int? OTPLength { get; set; }

    public IdentityInfo(IConfiguration config)
    {
        ConnectionString = config.GetConnectionString("Main");
        RedisConnectionString = config.GetConnectionString("Redis");
        var TokenProvider = config.GetSection("TokenProvider");
        Audience = TokenProvider.GetSection("Audience").Value;
        Issuer = TokenProvider.GetSection("Issuer").Value;
        SecretKey = TokenProvider.GetSection("SecretKey").Value;
        ExpirationTime = Convert.ToInt32(TokenProvider.GetSection("TokenExpireTime").Value);
        OTPLength = Convert.ToInt32(TokenProvider.GetSection("OtpLength").Value);
    }
}

