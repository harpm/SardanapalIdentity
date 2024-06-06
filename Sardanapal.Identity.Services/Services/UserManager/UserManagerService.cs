using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.Domain.Data;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Services.Statics;
using Sardanapal.ViewModel.Response;

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
    where TUR : class, IUserRoleBase<TUserKey, byte>, new()
{
    protected virtual byte _currentRole { get; }
    protected SdIdentityUnitOfWorkBase<TUserKey, byte, TUser, TRole, TUR> _context;
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

    public UserManagerService(SdIdentityUnitOfWorkBase<TUserKey, byte, TUser, TRole, TUR> context, ITokenService tokenService, byte curRole)
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

        var tokenRes = _tokenService.GenerateToken(user.Id, _currentRole);

        return tokenRes.StatusCode == StatusCode.Succeeded ? tokenRes.Data : string.Empty;
    }

    public Task<bool> HasRole(byte roleId, TUserKey userKey)
    {
        return this._context.UserRoles.AsNoTracking()
            .Where(x => x.UserId.Equals(userKey) && x.RoleId.Equals(roleId))
            .AnyAsync();
    }

    public async Task<TUserKey> RegisterUser(string username, string password)
    {
        var hashedPass = await Utilities.EncryptToMd5(password);

        var newUser = await this.GetUser(username);
        if (newUser == null)
        {
            newUser = new TUser()
            {
                Username = username,
                HashedPassword = hashedPass
            };

            await _context.AddAsync(newUser);
            await _context.SaveChangesAsync();
        }

        if (await HasRole(_currentRole, newUser.Id))
        {
            var roleUser = new TUR()
            {
                RoleId = _currentRole,
                UserId = newUser.Id
            };

            await _context.AddAsync(roleUser);
            await _context.SaveChangesAsync();
        }

        return newUser.Id;
    }
}