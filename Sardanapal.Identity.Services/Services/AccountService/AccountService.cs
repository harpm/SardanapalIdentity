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
}

public class AccountServiceBase<TUserKey, TUser, TRole, TUR, TLoginVM, TLoginDto, TRegisterVM>
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
    protected virtual string ServiceName { get; set; }
    protected readonly byte roleId;

    public AccountServiceBase(IUserManagerService<TUserKey, TUser, TRole> _userManagerService, byte _roleId)
    {
        this.userManagerService = _userManagerService;
        roleId = _roleId;
    }

    public async Task<IResponse<LoginDto>> Login(LoginVM model)
    {
        var result = new Response<LoginDto>();

        return await result.FillAsync(async () =>
        {
            string token = await userManagerService.Login(model.Username, model.Password);
            result.Set(StatusCode.Succeeded, new LoginDto() { Token = token });

            return result;
        });
    }

    public async Task<IResponse<TUserKey>> Register(TRegisterVM model)
    {
        var result = new Response<TUserKey>();

        return await result.FillAsync(async () =>
        {
            TUserKey userId = await userManagerService.RegisterUser(model.Username, model.Password);
            result.Set(StatusCode.Succeeded, userId);

            return result;
        });
    }
}