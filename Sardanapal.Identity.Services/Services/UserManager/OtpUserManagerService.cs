
using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Domain.Data;
using Sardanapal.Identity.Share.Resources;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Response;
using System.Data;

namespace Sardanapal.Identity.Services.Services.UserManager;

public class OtpUserManagerService<TOtpService, TUserKey, TUser, TRole, TClaim, TUR, TUC, TNewVM, TEditableVM, TValidateVM>
    : UserManager<TUserKey, TUser, TRole, TClaim, TUR, TUC>
    , IOtpUserManager<TUserKey, TUser, TRole, TClaim>
    where TOtpService : class, IOtpServiceBase<TUserKey, Guid, TNewVM, TValidateVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRole<byte>, new()
    where TClaim : class, IClaim<byte>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUC : class, IUserClaim<TUserKey, byte>, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
    where TValidateVM : ValidateOtpVM<TUserKey>, new()
{
    protected TOtpService OtpService { get; set; }

    public OtpUserManagerService(SdIdentityUnitOfWorkBase<TUserKey, byte, byte, TUser, TRole, TClaim, TUR, TUC> context
        , ITokenService tokenService
        , TOtpService _otpService)
        : base(context, tokenService)

    {
        OtpService = _otpService;
    }

    public virtual async Task<TUserKey> RequestLoginUser(long phonenumber, byte role)
    {
        var user = await this.Users
            .Where(x => x.PhoneNumber == phonenumber)
            .FirstOrDefaultAsync();

        if (user != null)
        {
            var otpRes = await OtpService.Add(new TNewVM()
            {
                UserId = user.Id,
                RoleId = role,
                Recipient = phonenumber.ToString()
            });

            if (otpRes.StatusCode != StatusCode.Succeeded)
            {
                throw new Exception(string.Join(", ", otpRes.DeveloperMessages));
            }

            return user.Id;
        }
        else
        {
            throw new KeyNotFoundException(Service_Messages.PhonenumberNotFound);
        }
    }

    public virtual async Task<TUserKey> RequestLoginUser(string email, byte role)
    {
        var user = await this.Users
            .Where(x => x.Email == email)
            .FirstOrDefaultAsync();

        if (user != null)
        {
            await OtpService.Add(new TNewVM()
            {
                UserId = user.Id,
                RoleId = role,
                Recipient = email
            });

            return user.Id;
        }
        else
        {
            throw new KeyNotFoundException(Service_Messages.EmailNotFound);
        }
    }

    public virtual async Task<TUserKey> RequestRegisterUser(long phonenumber, string firstname, string lastName, byte role)
    {
        var curUser = await Users
            .Where(x => x.PhoneNumber == phonenumber)
            .FirstOrDefaultAsync();

        if (curUser == null)
        {
            curUser = new TUser()
            {
                Username = phonenumber.ToString(),
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
            throw new DuplicateNameException(Service_Messages.DuplicatePhoneNumber);
        }

        if (curUser != null && !curUser.VerifiedPhoneNumber)
        {
            await OtpService.Add(new TNewVM()
            {
                Recipient = phonenumber.ToString(),
                UserId = curUser.Id,
                RoleId = role
            });
        }

        return curUser.Id;
    }

    public virtual async Task<TUserKey> RequestRegisterUser(string email, string firstname, string lastName, byte role)
    {
        var curUser = await Users
            .Where(x => x.Email == email)
            .FirstOrDefaultAsync();

        if (curUser == null)
        {
            curUser = new TUser()
            {
                Username = email,
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
            throw new DuplicateNameException(Service_Messages.DuplicateEmail);
        }

        if (curUser != null && !curUser.VerifiedEmail)
        {
            await OtpService.Add(new TNewVM()
            {
                Recipient = email,
                UserId = curUser.Id,
                RoleId = role
            });
        }

        return curUser.Id;
    }

    public virtual async Task<bool> VerifyRegisterOtpCode(string code, TUserKey id, byte roleId)
    {
        var curUser = await Users
            .Where(x => x.Id.Equals(id))
            .FirstOrDefaultAsync();

        if (curUser != null)
        {
            var validationRes = await OtpService.ValidateOtp(new TValidateVM { UserId = id, Code = code, RoleId = roleId });

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
            throw new KeyNotFoundException(Service_Messages.InvalidUserId);
        }
    }

    public virtual async Task<string> VerifyLoginOtpCode(string code, TUserKey id, byte roleId)
    {
        var curUser = await Users
            .Where(x => x.Id.Equals(id))
            .FirstOrDefaultAsync();

        if (curUser != null)
        {
            var validationRes = await OtpService.ValidateOtp(new TValidateVM
            {
                UserId = id,
                Code = code,
                RoleId = roleId
            });

            if (validationRes.StatusCode == StatusCode.Succeeded && validationRes.Data)
            {
                var tokenRes = _tokenService.GenerateToken(curUser.Username, [roleId], []);
                return tokenRes.StatusCode == StatusCode.Succeeded ? tokenRes.Data : string.Empty;
            }
            else
            {
                throw new Exception(string.Join(", "
                    , validationRes.DeveloperMessages
                    , "StatusCode: " + validationRes.StatusCode.ToString()));
            }
        }
        else
        {
            throw new Exception(Service_Messages.InvalidUserId);
        }
    }
}