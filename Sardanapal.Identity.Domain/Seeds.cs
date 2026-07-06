
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Share.Static;
using Sardanapal.Identity.Share.Statics;

namespace Sardanapal.Identity.Domain;

public static class IdentitySeeds
{
    public static IServiceProvider AddRoles<TRoleEnum, TRole, TRoleKey>(this IServiceProvider provider)
        where TRoleEnum : Enum
        where TRole : class, IRoleBase<TRoleKey>, new()
        where TRoleKey : IEquatable<TRoleKey>, IComparable<TRoleKey>
    {
        var scope = provider.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService(typeof(DbContext)) as DbContext;

        if (uow == null)
            throw new NullReferenceException(nameof(uow));
        

        foreach (TRoleKey roleEnumMember in typeof(TRoleEnum).GetEnumValues())
        {
            bool exists = uow.Set<TRole>()
                .AsNoTracking()
                .Any(r => r.Id.Equals(roleEnumMember));

            if (!exists)
            {
                uow.Add(new TRole()
                {
                    Id = roleEnumMember,
                    Title = Enum.GetName(typeof(TRoleEnum), roleEnumMember)
                });
            }
        }

        uow.SaveChanges();

        return provider;
    }

    public static async Task<IServiceProvider> AddAdminUser<TRoleEnum, TUser, TUserRole, TUserKey, TRoleKey>(this IServiceProvider provider)
        where TRoleEnum : Enum
        where TUser : class, IUser<TUserKey>, new()
        where TUserRole : class, IUserRole<TUserKey, TRoleKey>, new()
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
        where TRoleKey : IEquatable<TRoleKey>, IComparable<TRoleKey>
    {
        var scope = provider.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService(typeof(DbContext)) as DbContext;

        if (uow == null)
            throw new NullReferenceException(nameof(uow));

        SDConfigs? config = scope.ServiceProvider.GetService<IOptions<SDConfigs>>()?.Value;

        string username = !string.IsNullOrWhiteSpace(config?.SeedAdminUsername)
            ? config.SeedAdminUsername!
            : "admin";

        string? configuredPassword = config?.SeedAdminPassword;
        bool generatedPassword = string.IsNullOrWhiteSpace(configuredPassword);
        string password = generatedPassword
            ? Utilities.GenerateRandomPassword()
            : configuredPassword!;

        // Adding admin user
        if (!uow.Set<TUser>()
            .Where(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
            .AsNoTracking()
            .Any())
        {
            var hashedPassword = Utilities.HashPassword(password);

            TUser admin = new TUser()
            {
                Username = username,
                HashedPassword = hashedPassword,
                FirstName = "کاربر",
                LastName = "ارشد",
                Email = username,
                PhoneNumber = 0,
                VerifiedEmail = true,
                VerifiedPhoneNumber = true,
                MustChangePassword = true
            };

            await uow.AddAsync(admin);
            await uow.SaveChangesAsync();

            foreach (TRoleKey roleEnumMember in typeof(TRoleEnum).GetEnumValues())
            {
                uow.Add(new TUserRole()
                {
                    UserId = admin.Id,
                    RoleId = roleEnumMember,
                });
            }

            uow.SaveChanges();

            var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("Sardanapal.Identity.Seeds");

            if (generatedPassword)
            {
                logger?.LogWarning(
                    "Bootstrap admin created. Username: {Username} | Password: {Password} | A password change is required on first login.",
                    username, password);
            }
            else
            {
                logger?.LogInformation(
                    "Bootstrap admin '{Username}' created. A password change is required on first login.",
                    username);
            }
        }

        return provider;
    }
}
