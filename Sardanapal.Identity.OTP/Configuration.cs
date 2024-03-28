using Microsoft.Extensions.DependencyInjection;

namespace Sardanapal.Identity.OTP
{
    public static class Configuration
    {
        public static IServiceCollection AddOtpService(this IServiceCollection services, bool useCach)
        {
            return services;
        }
    }
}
