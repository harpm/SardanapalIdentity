
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Dto;
using Sardanapal.Identity.OTP.Services;
using Sardanapal.Identity.Services.Services;
using Sardanapal.Identity.Services.Services.AccountService;
using Sardanapal.Identity.Services.Services.UserManager;
using Sardanapal.Identity.Share;
using Sardanapal.Identity.ViewModel.Models.Account;

namespace Sardanapal.Identity.Services;

public static class Configurations
{
    public static IServiceCollection AddDefaultIdentityServices<TUserKey, TUser, TRole, TUR, TUserManager, TAccountService>(this IServiceCollection services)
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
        where TUser : class, IUserBase<TUserKey>, new()
        where TRole : class, IRoleBase<byte>, new()
        where TUR : class, IUserRoleBase<TUserKey, byte>, new()
        where TUserManager : class, IUserManagerService<TUserKey, TUser, TRole>, new()
        where TAccountService : class, IAccountServiceBase<TUserKey, LoginVM, LoginDto, RegisterVM>, new()
    {
        services.ConfigureIdentityOptions();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserManagerService<TUserKey, TUser, TRole>, TUserManager>();
        services.AddScoped<IAccountServiceBase<TUserKey, LoginVM, LoginDto, RegisterVM>, TAccountService>();

        return services;
    }

    public static IServiceCollection AddDefaultOtpIdentityServices<TContext, TUserKey, TUser, TRole, TUR, TUserManager, TAccountService>(this IServiceCollection services, bool useCach)
        where TContext : DbContext
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
        where TUser : class, IUserBase<TUserKey>, new()
        where TRole : class, IRoleBase<byte>, new()
        where TUR : class, IUserRoleBase<TUserKey, byte>, new()
        where TUserManager : class, IOtpUserManagerService<TUserKey, TUser, TRole>, new()
        where TAccountService : class, IOtpAccountServiceBase<TUserKey, LoginVM, LoginDto, RegisterVM>, new()
    {
        services.ConfigureIdentityOptions();
        services.AddScoped<ITokenService, TokenService>();

        services.AddOtpService<TContext, TUserKey>(useCach);

        services.AddScoped<IOtpUserManagerService<TUserKey, TUser, TRole>, TUserManager>();
        services.AddScoped<IOtpAccountServiceBase<TUserKey, LoginVM, LoginDto, RegisterVM>, TAccountService>();


        return services;
    }
}
