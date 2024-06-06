using Microsoft.IdentityModel.Tokens;

namespace Sardanapal.Identity.Share.Options;

public class IdentityInfo
{
    public string? ConnectionString { get; set; }
    public string? RedisConnectionString { get; set; }
    public TokenValidationParameters TokenParameters { get; set; }
    public int ExpirationTime { get; set; }
    public int? OTPLength { get; set; }
}

