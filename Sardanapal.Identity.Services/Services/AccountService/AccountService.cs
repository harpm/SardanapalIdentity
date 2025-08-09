using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.AccountService;

public abstract class AccountServiceBase<TUserManager, TUserKey, TUser, TLoginVM, TLoginDto, TRegisterVM>
    : IAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TUserManager : class, IUserManager<TUserKey, TUser>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TLoginVM : LoginVM, new()
    where TLoginDto : LoginDto, new()
    where TRegisterVM : RegisterVM, new()
{
    protected TUserManager userManagerService;
    protected virtual string ServiceName => "AccountService";
    protected abstract byte roleId { get; }

    public AccountServiceBase(TUserManager _userManagerService)
    {
        this.userManagerService = _userManagerService;
    }

    public virtual async Task<IResponse<TLoginDto>> Login(TLoginVM model)
    {
        IResponse<TLoginDto> result = new Response<TLoginDto>(ServiceName, OperationType.Fetch);

        return await result.FillAsync(async () =>
        {
            var tokenRes = await userManagerService.Login(model.Username, model.Password);

            if (tokenRes.IsSuccess)
            {
                result.Set(StatusCode.Succeeded, new TLoginDto() { Token = tokenRes.Data });
            }
            else if (tokenRes.StatusCode == StatusCode.Exception)
            {
                tokenRes.ConvertTo<TLoginDto>(result);
            }
            else
            {
                result.Set(StatusCode.NotExists);
            }
        });
    }

    public virtual async Task<IResponse<TUserKey>> Register(TRegisterVM model)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Add);

        return await result.FillAsync(async () =>
        {
            IResponse<TUserKey> userIdRes = await userManagerService.RegisterUser(model.Username, model.Password, this.roleId);

            if (userIdRes.IsSuccess)
            {
                result.Set(StatusCode.Succeeded, userIdRes.Data);
            }
            else
            {
                userIdRes.ConvertTo<TUserKey>(result);
            }
        });
    }

    public virtual async Task<IResponse<string>> RefreshToken(TUserKey userId)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Fetch);

        return await result.FillAsync(async () =>
        {
            var tokenRes = await userManagerService.RefreshToken(userId);
            if (tokenRes.IsSuccess)
            {
                result.Set(StatusCode.Succeeded, tokenRes.Data);
            }
            else if (tokenRes.StatusCode == StatusCode.Exception)
            {
                tokenRes.ConvertTo<string>(result);
            }
            else
            {
                result.Set(StatusCode.Failed);
            }
        });
    }
}