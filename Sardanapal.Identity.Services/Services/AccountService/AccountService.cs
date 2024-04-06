using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Dto;
using Sardanapal.Identity.Services.Services.UserManager;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Models.VM;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.AccountService;

public interface IAccountServiceBase<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto
{
    Task<IResponse<LoginDto>> Login(LoginVM Model);
}

public abstract class AccountServiceBase<TUserKey, TUser, TRole, TUR, TLoginVM, TLoginDto, TRegisterVM>
    : IAccountServiceBase<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : UserBase<TUserKey>, new()
    where TRole : RoleBase<byte, TUserKey>, new()
    where TUR : UserRoleBase<TUserKey>, new()
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto
{
    protected IUserManagerService<TUserKey, TUser, TRole> userManagerService;
    protected virtual string ServiceName { get; set; }
    protected readonly byte roleId;

    public AccountServiceBase(IUserManagerService<TUserKey, TUser, TRole> _userManagerService, byte _roleId)
    {
        roleId = _roleId;
    }

    public async Task<IResponse<LoginDto>> Login(LoginVM Model)
    {
        var result = new Response<LoginDto>();

        return await result.Create(async () =>
        {

            string token = await userManagerService.Login(Model.Username, Model.Password);
            result.Set(StatusCode.Succeeded, new LoginDto() { Token = token });

            return result;
        });
    }
}

public interface IOtpAccountServiceBase<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    Task<Response<TUserKey>> RequestOtp(OtpRequestVM Model);
    Task<Response<LoginDto>> LoginWithOtp(ValidateOtpVM<TUserKey> Model);
}

public abstract class OtpAccountServiceBase<TUserKey, TUser, TRole, TUR, TLoginVM, TLoginDto, TRegisterVM>
    : AccountServiceBase<TUserKey, TUser, TRole, TUR, TLoginVM, TLoginDto, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : UserBase<TUserKey>, new()
    where TRole : RoleBase<byte, TUserKey>, new()
    where TUR : UserRoleBase<TUserKey>, new()
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto
{

    public OtpAccountServiceBase(IOtpUserManagerService<TUserKey, TUser, TRole> _userManagerService, byte _roleId)
        : base(_userManagerService, _roleId)
    {

    }

    public async Task<IResponse<TUserKey>> RequestOtp(OtpRequestVM Model)
    {
        var result = new Response<TUserKey>(ServiceName, OperationType.Function);

        return await result.Create(async () =>
        {
            if (Model == null)
            {
                //var user = await userManagerService.GetUser(Model.Email, Model.PhoneNumber);
                //if (user != null)
                //{
                //    await OtpService.Add(new NewOtpVM<TUserKey>()
                //    {
                //        UserId = user.Id,
                //        RoleId = roleId
                //    });

                //    result.Set(StatusCode.Succeeded, user.Id);
                //}
                //else
                //{
                //    result.Set(StatusCode.Failed);
                //}
            }

            return result;
        });
    }

    public async Task<IResponse<LoginDto>> LoginWithOtp(ValidateOtpVM<TUserKey> Model)
    {
        var result = new Response<LoginDto>();

        return await result.Create(async () =>
        {
            //var otps = await OtpService.ValidateOtp(Model);
            //if (otps.Data)
            //{
            //    // TODO: should generate token

            //    result.Set(StatusCode.Succeeded);
            //}
            //else
            //{
            //    result.Set(StatusCode.Canceled);
            //}

            return result;
        });
    }
}