using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.Domain.Data;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.OTP.Services;
using Sardanapal.Identity.Services.Statics;
using Sardanapal.Identity.ViewModel.Models.VM;
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
    where TUser : UserBase<TUserKey>, new()
    where TRole : RoleBase<byte, TUserKey>, new()
    where TUR : UserRoleBase<TUserKey>, new()
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
        var username = await _context.Users.AsNoTracking()
            .Where(x => x.Id.Equals(userId) && x.UserRoles.Any(w => w.RoleId == _currentRole))
            .Select(x => x.Username)
            .FirstAsync();

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

public interface IOtpUserManagerService<TUserKey, TUser, TRole> : IUserManagerService<TUserKey, TUser, TRole>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUserBase<TUserKey>, new()
    where TRole : class, IRoleBase<byte>, new()
{
    Task<TUserKey> RequestRegisterUser(long phonenumber, string firstname, string lastName);
    Task<TUserKey> RequestRegisterUser(string email, string firstname, string lastName);
    Task<bool> VerifyOtpCode(string code, TUserKey id, byte roleId);
}

public class OtpUserManagerService<TUserKey, TUser, TRole, TUR> : UserManagerService<TUserKey, TUser, TRole, TUR>
    , IOtpUserManagerService<TUserKey, TUser, TRole>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : UserBase<TUserKey>, new()
    where TRole : RoleBase<byte, TUserKey>, new()
    where TUR : UserRoleBase<TUserKey>, new()
{
    protected IOtpService<TUserKey, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>> OtpService { get; set; }

    public OtpUserManagerService(SdIdentityUnitOfWorkBase<TUserKey, TUser, TRole, TUR> context, ITokenService tokenService, byte curRole
        , IOtpService<TUserKey, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>> _otpService) : base(context, tokenService, curRole)

    {
        OtpService = _otpService;
    }

    public async Task<TUserKey> RequestRegisterUser(long phonenumber, string firstname, string lastName)
    {
        var curUser = await Users
            .Where(x => x.PhoneNumber == phonenumber)
            .FirstOrDefaultAsync();

        if (curUser == null)
        {
            curUser = new TUser()
            {
                PhoneNumber = phonenumber,
                FirstName = firstname,
                LastName = lastName
            };

            await _context.AddAsync(curUser);
            await _context.SaveChangesAsync();

            return curUser.Id;
        }
        else if (curUser.VerifiedPhoneNumber)
        {
            throw new Exception("A User with this phone number already exists!");
        }

        if (curUser != null && !curUser.VerifiedPhoneNumber)
        {
            await OtpService.Add(new NewOtpVM<TUserKey>()
            {
                UserId = curUser.Id,
                RoleId = _currentRole
            });
        }

        return curUser.Id;
    }

    public async Task<TUserKey> RequestRegisterUser(string email, string firstname, string lastName)
    {
        var curUser = await Users
            .Where(x => x.Email == email)
            .FirstOrDefaultAsync();

        if (curUser == null)
        {
            curUser = new TUser()
            {
                Email = email,
                FirstName = firstname,
                LastName = lastName
            };

            await _context.AddAsync(curUser);
            await _context.SaveChangesAsync();

            return curUser.Id;
        }
        else if (curUser.VerifiedEmail)
        {
            throw new Exception("A User with this email already exists!");
        }

        if (curUser != null && !curUser.VerifiedEmail)
        {
            await OtpService.Add(new NewOtpVM<TUserKey>()
            {
                UserId = curUser.Id,
                RoleId = _currentRole
            });
        }

        return curUser.Id;
    }

    public async Task<bool> VerifyOtpCode(string code, TUserKey id, byte roleId)
    {
        var curUser = await Users
            .Where(x => x.Id.Equals(id))
            .FirstOrDefaultAsync();

        if (curUser != null)
        {
            var validationRes = await OtpService.ValidateOtp(new ValidateOtpVM<TUserKey> { UserId = id, Code = code, RoleId = roleId });

            if (validationRes.StatusCode == StatusCode.Succeeded && validationRes.Data)
            {
                if (!string.IsNullOrWhiteSpace(curUser.Email))
                {
                    curUser.VerifiedEmail = true;
                }
                else if (curUser.PhoneNumber.HasValue)
                {
                    curUser.VerifiedPhoneNumber = true;
                }

                _context.Set<TUser>().Update(curUser);
                await _context.SaveChangesAsync();

                return true;
            }

            return false;
        }
        else
        {
            throw new Exception("Invalid user id!");
        }
    }
}