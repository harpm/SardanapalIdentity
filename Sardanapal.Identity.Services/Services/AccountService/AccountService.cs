using Microsoft.Extensions.Logging;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.Share.Statics;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.AccountService;

public abstract class AccountServiceBase<TUserManager, TRoleManager, TUserKey, TUser, TRole, TUR, TUserSearchVM, TUserVM, TRegisterVM, TUserEditable>
    : IAccountService<TUserKey, TRegisterVM, TUserEditable>
    where TUserManager : IUserManager<TUserKey, TUser, TUserSearchVM, TUserVM, TRegisterVM, TUserEditable>
    where TRoleManager : IRoleManager<TUserKey, byte, TRole, TUR>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRoleBase<byte>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TUserVM : UserVM<TUserKey>, new()
    where TUserSearchVM : UserSearchVM, new()
    where TRegisterVM : RegisterVM<byte>, new()
    where TUserEditable : UserEditableVM, new()
{
    protected virtual string ServiceName => "AccountService";

    protected readonly TUserManager _userManager;
    protected readonly TRoleManager _roleManager;
    protected readonly ILogger _logger;

    public AccountServiceBase(TUserManager _userManager, TRoleManager roleManager, ILogger logger)
    {
        this._userManager = _userManager;
        this._roleManager = roleManager;
        this._logger = logger;
    }

    public virtual async Task<IResponse> Edit(TUserKey id, TUserEditable model)
    {
        return await _userManager.Edit(id, model);
    }

    public virtual Task<IResponse<TUserEditable>> GetEditable(TUserKey id) => _userManager.GetEditable(id);

    public virtual async Task<IResponse<LoginDto>> Login(LoginVM model)
    {
        IResponse<LoginDto> result = new Response<LoginDto>(ServiceName, OperationType.Fetch, _logger);

        return await result.FillAsync(async () =>
        {
            var userRes = await _userManager.GetUser(model.Username);
            if (userRes.IsSuccess)
            {
                if (userRes.Data.HashedPassword == await Utilities.EncryptToMd5(model.Password))
                {
                    var loginRes = await _userManager.Login(userRes.Data.Id);
                    if (loginRes.IsSuccess)
                    {
                        LoginDto loginDto = new LoginDto()
                        {
                            Token = loginRes.Data
                        };
                        result.Set(StatusCode.Succeeded, loginDto);
                    }
                    else
                    {
                        loginRes.ConvertTo<LoginDto>(result);
                    }
                }
                else
                {
                    result.Set(StatusCode.Failed, Identity_Messages.WrongPassword);
                }
            }
            else
            {
                result.Set(StatusCode.NotExists, Identity_Messages.InvalidUsername);
            }
        });
    }

    public virtual async Task<IResponse<TUserKey>> Register(TRegisterVM model)
    {
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Add, _logger);

        return await result.FillAsync(async () =>
        {
            IResponse<TUserKey> userIdRes = await _userManager.RegisterUser(model);

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

    public virtual async Task<IResponse> ChangePassword(ChangePasswordVM<TUserKey> model) =>
        await _userManager.ChangePassword(model.UserId, model.NewPassword);

    public virtual async Task<IResponse> ChangePassword(ChangePasswordVM model)
    {
        var result = new Response(ServiceName, OperationType.Edit, _logger);

        await result.FillAsync(async () =>
        {
            var userRes = await _userManager.GetUser(model.Username);
            if (userRes.IsSuccess)
            {
                var oldPass = await Utilities.EncryptToMd5(model.OldPassword);
                if (userRes.Data.HashedPassword == oldPass)
                {

                    if (model.NewPassword == model.OldPassword)
                    {
                        result.Set(StatusCode.Failed, Identity_Messages.DifferentPassword);
                    }
                    else
                    {
                        var changePassRes = await _userManager.ChangePassword(userRes.Data.Id, model.NewPassword);
                        if (changePassRes.IsSuccess)
                        {
                            result.Set(StatusCode.Succeeded, true);
                        }
                        else
                        {
                            changePassRes.ConvertTo<bool>(result);
                        }
                    }
                }
                else
                {
                    result.Set(StatusCode.Failed, Identity_Messages.WrongPassword);
                }
            }
            else
            {
                userRes.ConvertTo<bool>(result);
            }


        });

        return result;
    }

    public virtual async Task<IResponse<string>> RefreshToken(TUserKey userId)
    {
        IResponse<string> result = new Response<string>(ServiceName, OperationType.Fetch, _logger);

        return await result.FillAsync(async () =>
        {
            var tokenRes = await _userManager.RefreshToken(userId);
            if (tokenRes.IsSuccess)
            {
                result.Set(StatusCode.Succeeded, tokenRes.Data);
            }
            else
            {
                tokenRes.ConvertTo<string>(result);
            }
        });
    }

    public virtual async Task<IResponse> Delete(TUserKey userId)
    {
        return await _userManager.DeleteUser(userId);
    }

}
