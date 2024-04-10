﻿using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.Domain.Data;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Services.Statics;

namespace Sardanapal.Identity.Services.Services.UserManager;

public interface IUserManagerService<TUserKey, TUser, TRole>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUserBase<TUserKey>, new()
    where TRole : class, IRoleBase<byte>, new()
{
    Task<TUser?> GetUser(string? email = null, long? phoneNumber = null);
    Task<string> Login(string username, string password);
    Task<TUserKey> RegisterUser(string username, string password);

    void EditUserData(TUserKey id, string? username = null
        , string? password = null
        , long? phonenumber = null
        , string? email = null
        , string? firstname = null
        , string? lastname = null);
}

public class UserManagerService<TUserKey, TUser, TRole, TUR> : IUserManagerService<TUserKey, TUser, TRole>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUserBase<TUserKey>, new()
    where TRole : class, IRoleBase<byte>, new()
    where TUR : class, IUserRoleBase<TUserKey>, new()
{
    protected virtual byte _currentRole { get; }
    protected SdIdentityUnitOfWorkBase<TUserKey, TUser, TRole, TUR> _context;
    protected ITokenService _tokenService;

    public DbSet<TUser> Users
    {
        get
        {
            return _context.Set<TUser>();
        }
    }

    public DbSet<TRole> Roles
    {
        get
        {
            return _context.Set<TRole>();
        }
    }

    public UserManagerService(SdIdentityUnitOfWorkBase<TUserKey, TUser, TRole, TUR> context, ITokenService tokenService, byte curRole)
    {
        _context = context;
        _tokenService = tokenService;
        _currentRole = curRole;
    }

    public async Task<TUser?> GetUser(string? email = null, long? phoneNumber = null)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            return await Users.AsNoTracking()
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync();
        }
        else if (phoneNumber.HasValue)
        {
            return await Users.AsNoTracking()
                .Where(x => x.PhoneNumber == phoneNumber)
                .FirstOrDefaultAsync();
        }
        else
        {
            return null;
        }
    }

    public async void EditUserData(TUserKey id, string? username = null, string? password = null, long? phonenumber = null, string? email = null, string? firstname = null, string? lastname = null)
    {
        var user = await _context.Users.Where(x => x.Id.Equals(id)).FirstAsync();

        if (!string.IsNullOrWhiteSpace(username))
            user.Username = username;

        if (!string.IsNullOrWhiteSpace(password))
            user.HashedPassword = password;

        if (phonenumber.HasValue)
            user.PhoneNumber = phonenumber.Value;

        if (!string.IsNullOrWhiteSpace(email))
            user.Email = email;

        if (!string.IsNullOrWhiteSpace(firstname))
            user.FirstName = firstname;

        if (!string.IsNullOrWhiteSpace(lastname))
            user.LastName = lastname;

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<string> Login(string username, string password)
    {
        var md5Pass = await Utilities.EncryptToMd5(password);

        var user = await _context.Users.Where(x => x.Username == username
            && x.HashedPassword == md5Pass)
            .FirstAsync();

        string token = _tokenService.GenerateToken(username, _currentRole);

        return token;
    }

    public async Task<string> LoginViaOtp(TUserKey userId)
    {
        var username = await (from user in _context.Set<TUser>().AsNoTracking()
                       join ur in _context.Set<TUR>().AsNoTracking() on user.Id equals ur.UserId
                       where user.Id.Equals(userId) && ur.RoleId == _currentRole
                       select user.Username).FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(username))
            throw new NullReferenceException("The user is not available");

        string token = _tokenService.GenerateToken(username, _currentRole);

        return token;
    }

    public async Task<TUserKey> RegisterUser(string username, string password)
    {
        var hashedPass = await Utilities.EncryptToMd5(password);
        var newUser = new TUser()
        {
            Username = username,
            HashedPassword = hashedPass
        };

        await _context.AddAsync(newUser);
        await _context.SaveChangesAsync();

        return newUser.Id;
    }
}