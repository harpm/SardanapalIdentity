using Microsoft.Extensions.DependencyInjection;

namespace Sardanapal.Identity.Share;

public static class Configuration
{
    public static IServiceCollection ConfigureIdentityOptions(this IServiceCollection services)
    {
        return services;
    }
}
