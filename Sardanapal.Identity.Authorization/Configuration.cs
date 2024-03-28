using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Services.Services;

namespace Sardanapal.Identity.Authorization;

public static class Configuration
{
    public static IServiceCollection AddAuthServices<TTokenService>(this IServiceCollection services)
        where TTokenService : class, ITokenService
    {
        return services.AddScoped<ITokenService, TTokenService>();
    }
}
