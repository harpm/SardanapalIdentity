﻿
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Domain;

public static class IdentitySeeds
{
    public static IServiceProvider AddRoles<TRoleEnum, TRole, TRoleKey>(this IServiceProvider provider)
        where TRoleEnum : Enum
        where TRole : class, IRole<TRoleKey>, new()
        where TRoleKey : IEquatable<TRoleKey>, IComparable<TRoleKey>
    {
        var scope = provider.CreateScope();

        var uow = scope.ServiceProvider.GetRequiredService(typeof(DbContext)) as DbContext;

        if (uow == null)
            throw new NullReferenceException(nameof(uow));
        

        foreach (TRoleKey roleEnumMember in typeof(TRoleEnum).GetEnumValues())
        {
            uow.Add(new TRole()
            {
                Id = roleEnumMember,
                Title = Enum.GetName(typeof(TRoleEnum), roleEnumMember)
            });
        }

        uow.SaveChanges();

        return provider;
    }

    public static IServiceProvider AddAdminUser<TRoleEnum, TUser, TUserRole, TUserKey, TRoleKey>(this IServiceProvider provider)
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

        TUser admin;

        // Adding admin user
        if (!uow.Set<TUser>()
            .Where(u => u.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            .AsNoTracking()
            .Any())
        {
            admin = new TUser()
            {
                Username = "admin",
                HashedPassword = "admin", // this should be hashed first
                FirstName = "کاربر",
                LastName = "ارشد",
                Email = "admin",
                PhoneNumber = 0,
                VerifiedEmail = true,
                VerifiedPhoneNumber = true
            };

            uow.Add(admin);

            uow.SaveChanges();

            foreach (TRoleKey roleEnumMember in typeof(TRoleEnum).GetEnumValues())
            {
                uow.Add(new TUserRole()
                {
                    UserId = admin.Id,
                    RoleId = roleEnumMember,
                });
            }

            uow.SaveChanges();
        }

        return provider;
    }
}
