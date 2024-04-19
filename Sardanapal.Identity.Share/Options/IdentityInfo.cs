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

    public void SetConfigValues(IConfiguration config)
    {
        ConnectionString = config.GetConnectionString("Main");
        RedisConnectionString = config.GetConnectionString("Redis");
        var TokenProvider = config.GetSection("TokenProvider");
        Audience = TokenProvider.GetValue<string>("Audience");
        Issuer = TokenProvider.GetValue<string>("Issuer");
        SecretKey = TokenProvider.GetValue<string>("SecretKey");
        ExpirationTime = TokenProvider.GetValue<int>("TokenExpireTime");
        OTPLength = TokenProvider.GetValue<int>("OtpLength");
    }
}

