using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.OTP.VM;
using Sardanapal.Identity.OTP.Domain;

namespace Sardanapal.Identity.OTP.Services
{
    public static class Configuration
    {
        /// <summary>
        /// This will add OTP services to the DI container
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <typeparam name="TUserKey"></typeparam>
        /// <param name="services"></param>
        /// <param name="useCach">
        /// If true the Otp will be saved in cach
        /// otherwise otp codes will be saved in main database
        /// </param>
        /// <returns></returns>
        public static IServiceCollection AddOtpService<TContext, TUserKey>(this IServiceCollection services, bool useCach)
            where TContext : DbContext
            where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
        {
            if (useCach)
            {
                services.AddScoped<IOtpServiceBase<TUserKey, Guid, CachNewOtpVM<TUserKey, Guid>, ValidateOtpVM<TUserKey>>
                    , OtpCachService<TUserKey, Guid, OTPModel<TUserKey, Guid>>>();
            }
            else
            {
                services.AddScoped<IOtpServiceBase<TUserKey, Guid, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>>
                    , OtpService<TContext, TUserKey, Guid, OtpListItemVM<Guid>, OtpSearchVM, OtpVM, NewOtpVM<TUserKey>, OtpEditableVM<TUserKey>>>();
            }
            return services;
        }
    }
}
