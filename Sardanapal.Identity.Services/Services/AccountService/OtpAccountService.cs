﻿
using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Dto;
using Sardanapal.Identity.Services.Services.UserManager;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Models.VM;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.AccountService;
public interface IOtpAccountServiceBase<TUserKey, TLoginVM, TLoginDto, TRegisterVM> : IAccountServiceBase<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto
{
    Task<IResponse<TUserKey>> RequestLoginOtp(OtpLoginRequestVM Model);
    Task<IResponse<LoginDto>> LoginWithOtp(ValidateOtpVM<TUserKey> Model);
    Task<IResponse<TUserKey>> RequestRegisterOtp(OtpRegisterRequestVM model);
    Task<IResponse<bool>> RegisterWithOtp(ValidateOtpVM<TUserKey> Model);

}

public abstract class OtpAccountServiceBase<TUserKey, TUser, TRole, TUR, TLoginVM, TLoginDto, TRegisterVM>
    : AccountServiceBase<TUserKey, TUser, TRole, TUR, TLoginVM, TLoginDto, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUserBase<TUserKey>, new()
    where TRole : class, IRoleBase<byte>, new()
    where TUR : class, IUserRoleBase<TUserKey>, new()
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto
{
    protected IOtpUserManagerService<TUserKey, TUser, TRole> userManagerService;

    public OtpAccountServiceBase(IOtpUserManagerService<TUserKey, TUser, TRole> _userManagerService, byte _roleId)
        : base(_userManagerService, _roleId)
    {
        this.userManagerService = _userManagerService;
    }

    public async Task<IResponse<TUserKey>> RequestLoginOtp(OtpLoginRequestVM model)
    {
        var result = new Response<TUserKey>(ServiceName, OperationType.Function);

        return await result.Create(async () =>
        {
            dynamic identifier = model.PhoneNumber.HasValue ? model.PhoneNumber
                : !string.IsNullOrWhiteSpace(model.Email) ? model.Email : null;

            if (identifier != null)
            {
                var uid = await userManagerService
                    .RequestLoginUser(identifier);
                result.Set(StatusCode.Succeeded, uid);
            }
            else
            {
                result.Set(StatusCode.Canceled);
                result.DeveloperMessages = new string[] { "The email or phone number is entered incorrectly" };
            }

            return result;
        });
    }

    public async Task<IResponse<LoginDto>> LoginWithOtp(ValidateOtpVM<TUserKey> Model)
    {
        var result = new Response<LoginDto>();

        return await result.Create(async () =>
        {
            var token = await userManagerService.VerifyLoginOtpCode(Model.Code, Model.UserId, Model.RoleId);

            if (!string.IsNullOrWhiteSpace(token))
            {
                result.Set(StatusCode.Succeeded, new LoginDto()
                {
                    Token = token
                });
            }
            else
            {
                result.Set(StatusCode.Failed);
                result.DeveloperMessages = new string[] { "Failed generating token!" };
            }

            return result;
        });
    }

    public async Task<IResponse<TUserKey>> RequestRegisterOtp(OtpRegisterRequestVM model)
    {
        var result = new Response<TUserKey>();
        return await result.Create(async () =>
        {
            dynamic identifier = model.PhoneNumber.HasValue ? model.PhoneNumber
                : !string.IsNullOrWhiteSpace(model.Email) ? model.Email : null;

            if (identifier != null)
            {
                var uid = await userManagerService
                    .RequestRegisterUser(identifier, model.FirstName, model.LastName);
                result.Set(StatusCode.Succeeded, uid);
            }
            else
            {
                result.Set(StatusCode.Canceled);
                result.DeveloperMessages = new string[] { "The email or phone number is entered incorrectly" };
            }

            return result;
        });
    }

    public async Task<IResponse<bool>> RegisterWithOtp(ValidateOtpVM<TUserKey> Model)
    {
        var result = new Response<bool>();

        return await result.Create(async () =>
        {
            var isValid = await userManagerService.VerifyRegisterOtpCode(Model.Code, Model.UserId, Model.RoleId);
            if (isValid)
            {
                result.Set(StatusCode.Succeeded);
            }
            else
            {
                result.Set(StatusCode.Failed);
            }

            return result;
        });
    }
}
