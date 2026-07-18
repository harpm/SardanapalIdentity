using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Services.Services.AccountService;

namespace Sardanapal.Identity.Services;

public static class Configuration
{
    public static IServiceCollection AddSardanapalAccountLockout(this IServiceCollection services)
    {
        services.AddSingleton<ILoginAttemptTracker, LoginAttemptTracker>();
        return services;
    }
}
