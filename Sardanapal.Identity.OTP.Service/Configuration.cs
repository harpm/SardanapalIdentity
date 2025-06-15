using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Ef.Repository;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.ViewModel.Otp;

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
        public static IServiceCollection AddOtpService<TRepository, TOTPModel, TUserKey>(this IServiceCollection services, bool useCach)
            where TRepository : class, IOTPRepository<Guid, TOTPModel>
            where TOTPModel : class, IOTPModel<TUserKey, Guid>, new()
            where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
        {
            if (useCach)
            {
                services.AddScoped<IOtpServiceBase<TUserKey, Guid, CachNewOtpVM<TUserKey, Guid>, ValidateOtpVM<TUserKey>>
                    , OtpCachService<TUserKey, Guid, TOTPModel, CachNewOtpVM<TUserKey, Guid>, CachOtpEditableVM<TUserKey, Guid>, ValidateOtpVM<TUserKey>>>();
            }
            else
            {
                services.AddScoped<IOtpServiceBase<TUserKey, Guid, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>>
                    , OtpService<TRepository, TUserKey, Guid, TOTPModel, OtpListItemVM<Guid>, OtpSearchVM, OtpVM, NewOtpVM<TUserKey>, OtpEditableVM<TUserKey>, ValidateOtpVM<TUserKey>>>();
            }
            return services;
        }
    }
}
