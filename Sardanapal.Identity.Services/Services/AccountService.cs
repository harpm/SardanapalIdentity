using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Dto;
using Sardanapal.Identity.OTP.Models.Domain;
using Sardanapal.Identity.OTP.Services;
using Sardanapal.Identity.ViewModel.Models;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services;

public interface IAccountServiceBase<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    Task<Response<LoginDto>> Login(LoginVM Model);
    Task<Response<TUserKey>> RequestOtp(OtpRequestVM Model);
    Task<Response<LoginDto>> LoginWithOtp(ValidateOtpVM<TUserKey> Model);
}

public abstract class AccountServiceBase<TUserKey, TUser, TRole, TUR, TOtpCachModel> : IAccountServiceBase<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : UserBase<TUserKey>, new()
    where TRole : RoleBase<byte, TUserKey>, new()
    where TUR : UserRoleBase<TUserKey>, new()
    where TOtpCachModel : IOTPCode<TUserKey>, new()
{
    protected IUserManagerService<TUserKey, TUser, TRole> userManagerService;
    protected IOtpService<TUserKey, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>> OtpService { get; set; }
    protected virtual string ServiceName { get; set; }
    protected readonly byte roleId;

    public AccountServiceBase(IUserManagerService<TUserKey, TUser, TRole> _userManagerService
        , IOtpService<TUserKey, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>> _cacheService
        , byte _roleId)
    {
        roleId = _roleId;
    }

    public async Task<Response<LoginDto>> Login(LoginVM Model)
    {
        var result = new Response<LoginDto>();

        try
        {
            string token = await userManagerService.Login(Model.Username, Model.Password);
            result.Set(StatusCode.Succeeded, new LoginDto() { Token = token });
        }
        catch (Exception ex)
        {
            result.Set(StatusCode.Exception, ex);
        }

        return result;
    }

    public async Task<Response<TUserKey>> RequestOtp(OtpRequestVM Model)
    {
        var result = new Response<TUserKey>(ServiceName, OperationType.Function);

        try
        {
            if (Model == null)
            {
                var user = await userManagerService.GetUser(Model.Email, Model.PhoneNumber);
                if (user != null)
                {
                    await OtpService.Add(new NewOtpVM<TUserKey>()
                    {
                        UserId = user.Id,
                        RoleId = roleId
                    });

                    result.Set(StatusCode.Succeeded, user.Id);
                }
                else
                {
                    result.Set(StatusCode.Failed);
                }
            }
        }
        catch (Exception ex)
        {
            result.Set(StatusCode.Exception, ex);
        }

        return result;
    }

    public async Task<Response<LoginDto>> LoginWithOtp(ValidateOtpVM<TUserKey> Model)
    {
        var result = new Response<LoginDto>();

        try
        {
            var otps = await OtpService.ValidateOtp(Model);
            if (otps.Data)
            {
                // TODO: should generate token
                
                result.Set(StatusCode.Succeeded);
            }
            else
            {
                result.Set(StatusCode.Canceled);
            }
        }
        catch (Exception ex)
        {
            result.Set(StatusCode.Exception, ex);
        }

        return result;
    }
}