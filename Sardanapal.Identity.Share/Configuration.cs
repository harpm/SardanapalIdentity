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
        , int? otpCodeLen)
    {
        services.Configure<SDConfigs>(opt =>
        {
            opt.DbConnectionString = connString;
            opt.RedisConnectionString = redisConnString;
            opt.TokenParameters = tokenParams;
            opt.ExpirationTime = tokenExpireTime;
            opt.OTPLength = otpCodeLen;
        });

        return services;
    }
}
