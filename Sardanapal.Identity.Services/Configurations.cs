
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Dto;
using Sardanapal.Identity.OTP.Services;
using Sardanapal.Identity.Services.Services;
using Sardanapal.Identity.ViewModel.Models.Account;

namespace Sardanapal.Identity.Services;

public static class Configurations
{
    public static IServiceCollection AddDefaultIdentityServices<TUserKey, TUser, TRole, TUR, TUserManager, TAccountService>(this IServiceCollection services, byte roleId)
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
        where TUser : class, IUser<TUserKey>, new()
        where TRole : class, IRole<byte>, new()
        where TUR : class, IUserRole<TUserKey, byte>, new()
        where TUserManager : class, IUserManager<TUserKey, TUser, TRole>
        where TAccountService : class, IAccountService<TUserKey, LoginVM, LoginDto, RegisterVM>
    {
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserManager<TUserKey, TUser, TRole>, TUserManager>();
        services.AddScoped<IAccountService<TUserKey, LoginVM, LoginDto, RegisterVM>, TAccountService>();

        return services;
    }

    public static IServiceCollection AddDefaultOtpIdentityServices<TContext, TUserKey, TUser, TRole, TUR, TUserManager, TAccountService>(this IServiceCollection services, byte roleId, bool useCach)
        where TContext : DbContext
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
        where TUser : class, IUser<TUserKey>, new()
        where TRole : class, IRole<byte>, new()
        where TUR : class, IUserRole<TUserKey, byte>, new()
        where TUserManager : class, IOtpUserManager<TUserKey, TUser, TRole>, new()
        where TAccountService : class, IOtpAccountService<TUserKey, LoginVM, LoginDto, RegisterVM>, new()
    {
        services.AddScoped<ITokenService, TokenService>();

        services.AddOtpService<TContext, TUserKey>(useCach);

        services.AddScoped<IOtpUserManager<TUserKey, TUser, TRole>, TUserManager>();
        services.AddScoped<IOtpAccountService<TUserKey, LoginVM, LoginDto, RegisterVM>, TAccountService>();


        return services;
    }
}
