using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Services.Services;
using Microsoft.AspNetCore.Builder;
using Sardanapal.Identity.Authorization.Middlewares;

namespace Sardanapal.Identity.Authorization;

public static class Configuration
{
    /// <summary>
    /// Injecting Auth Filters
    /// </summary>
    /// <typeparam name="TTokenService"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddAuthServices<TTokenService>(this IServiceCollection services)
        where TTokenService : class, ITokenService
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITokenService, TTokenService>();
        return services;
    }

    /// <summary>
    /// Injecting Auth MiddleWares
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseSardanapalAuthentication(this IApplicationBuilder app)
    {
        app.UseMiddleware<SdAuthorizationMiddleware>();

        return app;
    }
}
