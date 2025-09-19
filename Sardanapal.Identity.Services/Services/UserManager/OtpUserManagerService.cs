
using System.Data;
using Microsoft.EntityFrameworkCore;
using Sardanapal.Contract.IRepository;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.UserManager;

public class EFOtpUserManagerService<TRepository, TOtpService, TUserKey, TUser, TUR, TUC, TNewVM, TEditableVM, TOTPLoginVM, TOTPRegisterVM>
    : EFUserManager<TRepository, TUserKey, TUser, TUR, TUC>
    , IOtpUserManager<TUserKey, TUser>
    where TRepository : IEFUserRepository<TUserKey, byte, TUser, TUR>, IEFCrudRepository<TUserKey, TUser>
    where TOtpService : class, IOtpServiceBase<TUserKey, Guid, TNewVM, TOTPLoginVM, TOTPRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUC : class, IUserClaim<TUserKey, byte>, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
    where TOTPLoginVM : OTPLoginVM<TUserKey>, new()
    where TOTPRegisterVM : OTPRegisterVM<TUserKey>, new()
{
    protected TOtpService OtpService { get; set; }

    public EFOtpUserManagerService(TRepository repository
        , ITokenService tokenService
        , TOtpService _otpService)
        : base(repository, tokenService)

    {
        OtpService = _otpService;
    }

    public virtual async Task<TUserKey> RequestLoginUser(long phonenumber, byte role)
    {
        var user = await _repository
            .FetchAll()
            .AsNoTracking()
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
            throw new KeyNotFoundException(Identity_Messages.PhonenumberNotFound);
        }
    }

    public virtual async Task<TUserKey> RequestLoginUser(string email, byte role)
    {
        var user = await _repository
            .FetchAll()
            .AsNoTracking()
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
            throw new KeyNotFoundException(Identity_Messages.EmailNotFound);
        }
    }

    public virtual async Task<TUserKey> RequestRegisterUser(long phonenumber, string firstname, string lastName, byte role)
    {
        var curUser = await _repository
            .FetchAll()
            .AsNoTracking()
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

            await _repository.AddAsync(curUser);
            await _repository.SaveChangesAsync();

            return curUser.Id;
        }
        else if (curUser.VerifiedPhoneNumber)
        {
            throw new DuplicateNameException(Identity_Messages.DuplicatePhoneNumber);
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
        var curUser = await _repository
            .FetchAll()
            .AsNoTracking()
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

            await _repository.AddAsync(curUser);
            await _repository.SaveChangesAsync();

            return curUser.Id;
        }
        else if (curUser.VerifiedEmail)
        {
            throw new DuplicateNameException(Identity_Messages.DuplicateEmail);
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
        var curUser = await _repository
            .FetchAll()
            .AsNoTracking()
            .Where(x => x.Id.Equals(id))
            .FirstOrDefaultAsync();

        if (curUser != null)
        {
            var validationRes = await OtpService.ValidateOtpRegister(new TOTPRegisterVM { UserId = id, Code = code, RoleId = roleId });

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

                await _repository.UpdateAsync(id, curUser);
                await _repository.SaveChangesAsync();

                return true;
            }

            return false;
        }
        else
        {
            throw new KeyNotFoundException(Identity_Messages.InvalidUserId);
        }
    }

    public virtual async Task<string> VerifyLoginOtpCode(string code, TUserKey id, byte roleId)
    {
        var curUser = await _repository
            .FetchAll()
            .AsNoTracking()
            .Where(x => x.Id.Equals(id))
            .FirstOrDefaultAsync();

        if (curUser != null)
        {
            var validationRes = await OtpService.ValidateOtpLogin(new TOTPLoginVM
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
            throw new Exception(Identity_Messages.InvalidUserId);
        }
    }
}

public class OtpUserManagerService<TRepository, TOtpService, TUserKey, TUser, TUR, TUC, TNewVM, TEditableVM, TOTPLoginVM, TOTPRegisterVM>
    : UserManager<TRepository, TUserKey, TUser, TUR, TUC>
    , IOtpUserManager<TUserKey, TUser>
    where TRepository : IUserRepository<TUserKey, byte, TUser, TUR>, IMemoryRepository<TUserKey, TUser>
    where TOtpService : class, IOtpServiceBase<TUserKey, Guid, TNewVM, TOTPLoginVM, TOTPRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUC : class, IUserClaim<TUserKey, byte>, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
    where TOTPLoginVM : OTPLoginVM<TUserKey>, new()
    where TOTPRegisterVM : OTPRegisterVM<TUserKey>, new()
{
    protected TOtpService OtpService { get; set; }

    public OtpUserManagerService(TRepository repository
        , ITokenService tokenService
        , TOtpService _otpService)
        : base(repository, tokenService)

    {
        OtpService = _otpService;
    }

    public virtual async Task<TUserKey> RequestLoginUser(long phonenumber, byte role)
    {
        var user = _repository
            .FetchAll()
            .Where(x => x.PhoneNumber == phonenumber)
            .FirstOrDefault();

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
            throw new KeyNotFoundException(Identity_Messages.PhonenumberNotFound);
        }
    }

    public virtual async Task<TUserKey> RequestLoginUser(string email, byte role)
    {
        var user = _repository
            .FetchAll()
            .Where(x => x.Email == email)
            .FirstOrDefault();

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
            throw new KeyNotFoundException(Identity_Messages.EmailNotFound);
        }
    }

    public virtual async Task<TUserKey> RequestRegisterUser(long phonenumber, string firstname, string lastName, byte role)
    {
        var curUser = _repository
            .FetchAll()
            .Where(x => x.PhoneNumber == phonenumber)
            .FirstOrDefault();

        if (curUser == null)
        {
            curUser = new TUser()
            {
                Username = phonenumber.ToString(),
                PhoneNumber = phonenumber,
                FirstName = firstname,
                LastName = lastName
            };

            await _repository.AddAsync(curUser);

            return curUser.Id;
        }
        else if (curUser.VerifiedPhoneNumber)
        {
            throw new DuplicateNameException(Identity_Messages.DuplicatePhoneNumber);
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
        var curUser = _repository
            .FetchAll()
            .Where(x => x.Email == email)
            .FirstOrDefault();

        if (curUser == null)
        {
            curUser = new TUser()
            {
                Username = email,
                Email = email,
                FirstName = firstname,
                LastName = lastName
            };

            await _repository.AddAsync(curUser);

            return curUser.Id;
        }
        else if (curUser.VerifiedEmail)
        {
            throw new DuplicateNameException(Identity_Messages.DuplicateEmail);
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
        var curUser = _repository
            .FetchAll()
            .Where(x => x.Id.Equals(id))
            .FirstOrDefault();

        if (curUser != null)
        {
            var validationRes = await OtpService.ValidateOtpRegister(new TOTPRegisterVM { UserId = id, Code = code, RoleId = roleId });

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

                await _repository.UpdateAsync(id, curUser);

                return true;
            }

            return false;
        }
        else
        {
            throw new KeyNotFoundException(Identity_Messages.InvalidUserId);
        }
    }

    public virtual async Task<string> VerifyLoginOtpCode(string code, TUserKey id, byte roleId)
    {
        var curUser = _repository
            .FetchAll()
            .Where(x => x.Id.Equals(id))
            .FirstOrDefault();

        if (curUser != null)
        {
            var validationRes = await OtpService.ValidateOtpLogin(new TOTPLoginVM
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
            throw new Exception(Identity_Messages.InvalidUserId);
        }
    }
}
