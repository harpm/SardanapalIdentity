using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Dto;
using Sardanapal.Identity.OTP.Models.Cach;
using Sardanapal.Identity.OTP.Services;
using Sardanapal.Identity.ViewModel.Models;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services;

public interface IAccountServiceBase<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    Task<Response<LoginDto>> Login(LoginVM Model);
    Task<Response<TKey>> RequestOtp(OtpRequestVM Model);
    Task<Response<LoginDto>> LoginWithOtp(OtpLoginVM Model);
}

public abstract class AccountServiceBase<TKey, TUser, TRole, TOtpCachModel> : IAccountServiceBase<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TUser : IUserBase<TKey>, new()
    where TRole : IRoleBase<byte>, new()
    where TOtpCachModel : OtpCachModel<TKey>, new()
{
    protected IUserManagerService<TKey, TUser, TRole> userManagerService;
    protected IOtpCachService<TKey, TOtpCachModel> CacheService { get; set; }
    protected virtual string ServiceName { get; set; }
    protected readonly byte roleId;

    public AccountServiceBase(IUserManagerService<TKey, TUser, TRole> _userManagerService
        , IOtpCachService<TKey, TOtpCachModel> _cacheService
        , OtpService _otpService
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

    public async Task<Response<TKey>> RequestOtp(OtpRequestVM Model)
    {
        var result = new Response<TKey>(ServiceName, OperationType.Function);

        try
        {
            if (Model == null)
            {
                var user = await userManagerService.GetUser(Model.Email, Model.PhoneNumber);
                if (user != null)
                {
                    await CacheService.Add(new TOtpCachModel()
                    {
                        Id = user.Id,
                        Role = roleId
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

    public async Task<Response<LoginDto>> LoginWithOtp(OtpLoginVM Model)
    {
        var result = new Response<LoginDto>();

        try
        {
            var otps = await CacheService.GetAll();
            if (otps.Value
                .Where(x => x.Code == Model.OtpCode && x.Role == roleId && x.ExpireTime < DateTime.UtcNow)
                .Any())
            {
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