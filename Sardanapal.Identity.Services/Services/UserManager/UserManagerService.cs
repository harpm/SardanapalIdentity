using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Domain.Data;
using Sardanapal.Identity.Services.Statics;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.UserManager;

public class UserManager<TUserKey, TUser, TRole, TClaim, TUR, TUC> : IUserManager<TUserKey, TUser, TRole, TClaim>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRole<byte>, new()
    where TClaim : class, IClaim<byte>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUC : class, IUserClaim<TUserKey, byte>, new()
{
    protected SdIdentityUnitOfWorkBase<TUserKey, byte, byte, TUser, TRole, TClaim, TUR, TUC> _context;
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

    public UserManager(SdIdentityUnitOfWorkBase<TUserKey, byte, byte, TUser, TRole, TClaim, TUR, TUC> context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public virtual async Task<TUser?> GetUser(string? email = null, long? phoneNumber = null)
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

    public virtual async void EditUserData(TUserKey id, string? username = null, string? password = null, long? phonenumber = null, string? email = null, string? firstname = null, string? lastname = null)
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

    public virtual async Task<string> Login(string username, string password)
    {
        var md5Pass = await Utilities.EncryptToMd5(password);

        var user = await _context.Users.Where(x => x.Username == username
            && x.HashedPassword == md5Pass)
            .FirstAsync();

        var roles = await _context.UserRoles.AsNoTracking()
            .Where(x => x.UserId.Equals(user.Id))
            .Select(x => x.RoleId)
            .ToListAsync();

        var tokenRes = _tokenService.GenerateToken(user.Id.ToString(), roles.ToArray(), []);

        return tokenRes.StatusCode == StatusCode.Succeeded ? tokenRes.Data : string.Empty;
    }

    public virtual Task<bool> HasRole(byte roleId, TUserKey userKey)
    {
        return this._context.UserRoles.AsNoTracking()
            .Where(x => x.UserId.Equals(userKey) && x.RoleId.Equals(roleId))
            .AnyAsync();
    }

    public virtual async Task<TUserKey> RegisterUser(string username, string password, byte role)
    {
        var hashedPass = await Utilities.EncryptToMd5(password);

        var newUser = await GetUser(username);
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

        if (await HasRole(role, newUser.Id))
        {
            var roleUser = new TUR()
            {
                RoleId = role,
                UserId = newUser.Id
            };

            await _context.AddAsync(roleUser);
            await _context.SaveChangesAsync();
        }

        return newUser.Id;
    }

    public async Task<string> RefreshToken(TUserKey userId)
    {
        string result = null;

        var roles = await _context.UserRoles.AsNoTracking()
            .Where(x => x.UserId.Equals(userId))
            .Select(x => x.RoleId)
            .ToArrayAsync();

        if (roles != null && roles.Any())
        {
            var resultModel = _tokenService.GenerateToken(userId.ToString(), roles, []);

            if (resultModel.StatusCode == StatusCode.Succeeded)
                result = resultModel.Data;
        }

        return result;
    }
}