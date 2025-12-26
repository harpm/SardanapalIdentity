using Microsoft.Extensions.Logging;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.Share.Statics;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.AccountService;

public abstract class AccountServiceBase<TUserManager, TUserKey, TUser, TLoginVM, TLoginDto, TRegisterVM, TUserEditable>
    : IAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM, TUserEditable>
    where TUserManager : class, IUserManager<TUserKey, TUser, TRegisterVM, TUserEditable>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TLoginVM : LoginVM, new()
    where TLoginDto : LoginDto, new()
    where TRegisterVM : RegisterVM<byte>, new()
    where TUserEditable : UserEditableVM, new()
{
    protected virtual string ServiceName => "AccountService";

    protected readonly TUserManager _userManagerService;
    protected readonly ILogger _logger;

    public AccountServiceBase(TUserManager _userManagerService, ILogger logger)
    {
        this._userManagerService = _userManagerService;
        this._logger = logger;
    }

    public virtual async Task<IResponse> Edit(TUserKey id, TUserEditable model)
    {
        return await _userManagerService.Edit(id, model);
    }
    public virtual async Task<IResponse<TUserEditable>> GetEditable(TUserKey id)
    {
        return await _userManagerService.GetUserEditable(id);
    }
    public virtual async Task<IResponse<TLoginDto>> Login(TLoginVM model)
    {
        IResponse<TLoginDto> result = new Response<TLoginDto>(ServiceName, OperationType.Fetch, _logger);

        return await result.FillAsync(async () =>
        {
            var tokenRes = await _userManagerService.Login(model.Username, model.Password);

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
        IResponse<TUserKey> result = new Response<TUserKey>(ServiceName, OperationType.Add, _logger);

        return await result.FillAsync(async () =>
        {
            IResponse<TUserKey> userIdRes = await _userManagerService.RegisterUser(model);

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
        await _userManagerService.ChangePassword(model.UserId, model.NewPassword);

    public virtual async Task<IResponse> ChangePassword(ChangePasswordVM model)
    {
        var result = new Response(ServiceName, OperationType.Edit, _logger);

        await result.FillAsync(async () =>
        {
            var userRes = await _userManagerService.GetUser(model.Username);
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
                        var changePassRes = await _userManagerService.ChangePassword(userRes.Data.Id, model.NewPassword);
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
            var tokenRes = await _userManagerService.RefreshToken(userId);
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
        return await _userManagerService.DeleteUser(userId);
    }

}
