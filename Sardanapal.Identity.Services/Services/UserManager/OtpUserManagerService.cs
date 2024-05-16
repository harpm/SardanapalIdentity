
using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.Domain.Data;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.OTP.Services;
using Sardanapal.Identity.OTP.VM;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.UserManager;


public interface IOtpUserManagerService<TUserKey, TUser, TRole> : IUserManagerService<TUserKey, TUser, TRole>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUserBase<TUserKey>, new()
    where TRole : class, IRoleBase<byte>, new()
{
    Task<TUserKey> RequestLoginUser(long phonenumber);
    Task<TUserKey> RequestLoginUser(string email);
    Task<TUserKey> RequestRegisterUser(long phonenumber, string firstname, string lastName);
    Task<TUserKey> RequestRegisterUser(string email, string firstname, string lastName);
    Task<bool> VerifyRegisterOtpCode(string code, TUserKey id, byte roleId);
    Task<string> VerifyLoginOtpCode(string code, TUserKey id, byte roleId);
}

public class OtpUserManagerService<TUserKey, TUser, TRole, TUR> : UserManagerService<TUserKey, TUser, TRole, TUR>
    , IOtpUserManagerService<TUserKey, TUser, TRole>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : UserBase<TUserKey>, new()
    where TRole : RoleBase<byte>, new()
    where TUR : UserRoleBase<TUserKey, byte>, new()
{
    protected IOtpService<TUserKey, Guid, OtpSearchVM, OtpVM, NewOtpVM<TUserKey>, OtpEditableVM<TUserKey>> OtpService { get; set; }

    public OtpUserManagerService(SdIdentityUnitOfWorkBase<TUserKey, byte, TUser, TRole, TUR> context, ITokenService tokenService, byte curRole
        , IOtpService<TUserKey, Guid, OtpSearchVM, OtpVM, NewOtpVM<TUserKey>, OtpEditableVM<TUserKey>> _otpService) : base(context, tokenService, curRole)

    {
        OtpService = _otpService;
    }

    public async Task<TUserKey> RequestLoginUser(long phonenumber)
    {
        var user = await this.Users
            .Where(x => x.PhoneNumber == phonenumber)
            .FirstOrDefaultAsync();

        if (user != null)
        {
            await OtpService.Add(new NewOtpVM<TUserKey>()
            {
                UserId = user.Id,
                RoleId = _currentRole
            });

            return user.Id;
        }
        else
        {
            throw new Exception("User phone number not found!");
        }
    }

    public async Task<TUserKey> RequestLoginUser(string email)
    {
        var user = await this.Users
            .Where(x => x.Email == email)
            .FirstOrDefaultAsync();

        if (user != null)
        {
            await OtpService.Add(new NewOtpVM<TUserKey>()
            {
                UserId = user.Id,
                RoleId = _currentRole
            });

            return user.Id;
        }
        else
        {
            throw new Exception("User email not found!");
        }
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

    public async Task<bool> VerifyRegisterOtpCode(string code, TUserKey id, byte roleId)
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
                else
                {
                    return false;
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

    public async Task<string> VerifyLoginOtpCode(string code, TUserKey id, byte roleId)
    {
        var curUser = await Users
            .Where(x => x.Id.Equals(id))
            .FirstOrDefaultAsync();

        if (curUser != null)
        {
            var validationRes = await OtpService.ValidateOtp(new ValidateOtpVM<TUserKey>
            {
                UserId = id,
                Code = code,
                RoleId = roleId
            });

            if (validationRes.StatusCode == StatusCode.Succeeded && validationRes.Data)
            {
                var tokenRes = _tokenService.GenerateToken(curUser.Username, _currentRole);
                return tokenRes.StatusCode == StatusCode.Succeeded ? tokenRes.Data : string.Empty;
            }

            return string.Empty;
        }
        else
        {
            throw new Exception("Invalid user id!");
        }
    }
}