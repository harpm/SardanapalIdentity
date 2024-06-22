using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Dto;
using Sardanapal.Identity.Services.Services.UserManager;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.AccountService;

public interface IAccountServiceBase<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto
    where TRegisterVM : RegisterVM
{
    Task<IResponse<LoginDto>> Login(LoginVM model);
    Task<IResponse<TUserKey>> Register(TRegisterVM model);
    Task<IResponse<string>> RefreshToken(TUserKey userId);

}

public abstract class AccountServiceBase<TUserKey, TUser, TRole, TUR, TLoginVM, TLoginDto, TRegisterVM>
    : IAccountServiceBase<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUserBase<TUserKey>, new()
    where TRole : class, IRoleBase<byte>, new()
    where TUR : class, IUserRoleBase<TUserKey, byte>, new()
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto
    where TRegisterVM : RegisterVM
{
    protected IUserManagerService<TUserKey, TUser, TRole> userManagerService;
    protected virtual string ServiceName => "AccountService";
    protected abstract byte roleId { get; }

    public AccountServiceBase(IUserManagerService<TUserKey, TUser, TRole> _userManagerService)
    {
        this.userManagerService = _userManagerService;
    }

    public virtual async Task<IResponse<LoginDto>> Login(LoginVM model)
    {
        var result = new Response<LoginDto>();

        return await result.FillAsync(async () =>
        {
            string token = await userManagerService.Login(model.Username, model.Password);

            if (!string.IsNullOrWhiteSpace(token))
            {
                result.Set(StatusCode.Succeeded, new LoginDto() { Token = token });
            }
            else
            {
                result.Set(StatusCode.NotExists);
            }
        });
    }

    public virtual async Task<IResponse<TUserKey>> Register(TRegisterVM model)
    {
        var result = new Response<TUserKey>();

        return await result.FillAsync(async () =>
        {
            TUserKey userId = await userManagerService.RegisterUser(model.Username, model.Password, this.roleId);
            result.Set(StatusCode.Succeeded, userId);
        });
    }

    public virtual async Task<IResponse<string>> RefreshToken(TUserKey userId)
    {
        var result = new Response<string>();

        return await result.FillAsync(async () =>
        {
            string token = await userManagerService.RefreshToken(userId);
            if (!string.IsNullOrWhiteSpace(token))
            {
                result.Set(StatusCode.Succeeded, token);
            }
            else
            {
                result.Set(StatusCode.Failed);
            }
        });
    }
}