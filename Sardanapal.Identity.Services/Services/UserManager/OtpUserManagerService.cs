﻿
using Microsoft.EntityFrameworkCore;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Domain.Data;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.UserManager;

public class OtpUserManagerService<TOtpService, TUserKey, TUser, TRole, TUR, TNewVM, TEditableVM, TValidateVM>
    : UserManager<TUserKey, TUser, TRole, TUR>
    , IOtpUserManager<TUserKey, TUser, TRole>
    where TOtpService : class, IOtpServiceBase<TUserKey, Guid, TNewVM, TValidateVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRole<byte>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
    where TValidateVM : ValidateOtpVM<TUserKey>, new()
{
    protected TOtpService OtpService { get; set; }

    public OtpUserManagerService(SdIdentityUnitOfWorkBase<TUserKey, byte, TUser, TRole, TUR> context
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
            await OtpService.Add(new TNewVM()
            {
                UserId = user.Id,
                RoleId = role
            });

            return user.Id;
        }
        else
        {
            throw new Exception("User phone number not found!");
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
                RoleId = role
            });

            return user.Id;
        }
        else
        {
            throw new Exception("User email not found!");
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
            throw new Exception("A User with this phone number already exists!");
        }

        if (curUser != null && !curUser.VerifiedPhoneNumber)
        {
            await OtpService.Add(new TNewVM()
            {
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
            throw new Exception("A User with this email already exists!");
        }

        if (curUser != null && !curUser.VerifiedEmail)
        {
            await OtpService.Add(new TNewVM()
            {
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
            throw new Exception("Invalid user id!");
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
                var tokenRes = _tokenService.GenerateToken(curUser.Username, roleId);
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