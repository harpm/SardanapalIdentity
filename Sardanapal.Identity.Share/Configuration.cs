using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Sardanapal.Identity.Share.Static;

namespace Sardanapal.Identity.Share;

public static class Configuration
{
    public static IServiceCollection ConfigureIdentityOptions(this IServiceCollection services
        , string connString
        , string redisConnString
        , TokenValidationParameters tokenParams
        , int tokenExpireTime
        , int otpCodeLen)
    {
        StaticConfigs.DbConnectionString = connString;
        StaticConfigs.RedisConnectionString = redisConnString;
        StaticConfigs.TokenParameters = tokenParams;
        StaticConfigs.ExpirationTime = tokenExpireTime;
        StaticConfigs.OTPLength = otpCodeLen;

        return services;
    }
}
