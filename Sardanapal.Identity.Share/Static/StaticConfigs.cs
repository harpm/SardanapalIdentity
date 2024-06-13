
using Microsoft.IdentityModel.Tokens;

namespace Sardanapal.Identity.Share.Static;

public static class StaticConfigs
{
    public static string DbConnectionString { get; set; }
    public static string? RedisConnectionString { get; set; }
    public static TokenValidationParameters TokenParameters { get; set; }
    public static int ExpirationTime { get; set; }
    public static int? OTPLength { get; set; }
}
