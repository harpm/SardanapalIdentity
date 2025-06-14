﻿using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Dto;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services.AccountService;

public abstract class AccountServiceBase<TUserManager, TUserKey, TUser, TRole, TClaim, TUR, TLoginVM, TLoginDto, TRegisterVM>
    : IAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TUserManager : class, IUserManager<TUserKey, TUser, TRole, TClaim>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRole<byte>, new()
    where TClaim : class, IClaim<byte>, new()
    where TUR : class, IUserRole<TUserKey, byte>, new()
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto, new()
    where TRegisterVM : RegisterVM
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
        var result = new Response<TLoginDto>();

        return await result.FillAsync(async () =>
        {
            string token = await userManagerService.Login(model.Username, model.Password);

            if (!string.IsNullOrWhiteSpace(token))
            {
                result.Set(StatusCode.Succeeded, new TLoginDto() { Token = token });
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