
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Sardanapal.Identity.Share.Static;

namespace Sardanapal.Identity.Share;

public static class Configuration
{
    public static IServiceCollection ConfigureIdentityOptions(this IServiceCollection services
        , string connString
        , string? redisConnString
        , TokenValidationParameters tokenParams
        , int tokenExpireTime
        , int? otpCodeLen
        , string? seedAdminUsername = null
        , string? seedAdminPassword = null
        , int tokenRefreshThresholdMinutes = 10)
    {
        services.Configure<SDConfigs>(opt =>
        {
            opt.DbConnectionString = connString;
            opt.RedisConnectionString = redisConnString;
            opt.TokenParameters = tokenParams;
            opt.ExpirationTime = tokenExpireTime;
            opt.OTPLength = otpCodeLen;
            opt.SeedAdminUsername = seedAdminUsername;
            opt.SeedAdminPassword = seedAdminPassword;
            opt.TokenRefreshThresholdMinutes = tokenRefreshThresholdMinutes;
        });

        return services;
    }
}
