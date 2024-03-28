using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.OTP.Models.Cach;
using Sardanapal.Identity.OTP.Services;
using Sardanapal.Identity.ViewModel.Models;

namespace Sardanapal.Identity.OTP
{
    public static class Configuration
    {
        public static IServiceCollection AddOtpService<TUnitOfWork, TUserKey>(this IServiceCollection services, bool useCach)
            where TUnitOfWork : DbContext
            where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
        {
            if (useCach)
            {
                services.AddScoped<IOtpService<TUserKey, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>>
                    , OtpCachService<TUserKey, OtpCachModel<Guid>>>();
            }
            else
            {
                services.AddScoped<IOtpService<TUserKey, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>>
                    , OtpService<TUnitOfWork, TUserKey, OtpListItemVM<Guid>, OtpSearchVM, OtpVM<TUserKey>, NewOtpVM<TUserKey>, OtpEditableVM<TUserKey>>>();
            }
            return services;
        }
    }
}
