using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Sardanapal.Identity.Share.Options;

namespace Sardanapal.Identity.Share;

public static class Configuration
{
    public static IServiceCollection ConfigureIdentityOptions(this IServiceCollection services
        , string connectionString
        , string redisConnection
        , TokenValidationParameters validationParams
        , int otpExpTime
        , int otpLength)
    {
        services.AddOptions<IdentityInfo>().Configure(i =>
        {
            i.ConnectionString = connectionString;
            i.RedisConnectionString = redisConnection;
            i.OTPLength = otpLength;
            i.TokenParameters = validationParams;
            i.ExpirationTime = otpExpTime;
        });
        return services;
    }
}
