using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.OTP.Models.Domain;
using Sardanapal.Identity.OTP.Services;
using Sardanapal.Identity.ViewModel.Models.VM;

namespace Sardanapal.Identity.OTP
{
    public static class Configuration
    {
        /// <summary>
        /// This will add otp services to the DI
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
                services.AddScoped<IOtpService<TUserKey, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>>
                    , OtpCachService<TUserKey, OTPModel<Guid>>>();
            }
            else
            {
                services.AddScoped<IOtpService<TUserKey, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>>
                    , OtpService<TContext, TUserKey, OtpListItemVM<Guid>, OtpSearchVM, OtpVM, NewOtpVM<TUserKey>, OtpEditableVM<TUserKey>>>();
            }
            return services;
        }
    }
}
