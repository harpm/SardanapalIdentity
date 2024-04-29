using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Share.Options;

namespace Sardanapal.Identity.Share;

public static class Configuration
{
    public static IServiceCollection ConfigureIdentityOptions(this IServiceCollection services)
    {
        services.AddSingleton<IdentityInfo>();
        return services;
    }
}
