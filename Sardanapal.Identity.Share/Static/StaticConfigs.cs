
using Microsoft.IdentityModel.Tokens;

namespace Sardanapal.Identity.Share.Static;

public record StaticConfigs
{
    public string DbConnectionString { get; set; }
    public string? RedisConnectionString { get; set; }
    public TokenValidationParameters TokenParameters { get; set; }
    public int ExpirationTime { get; set; }
    public int? OTPLength { get; set; }
}
