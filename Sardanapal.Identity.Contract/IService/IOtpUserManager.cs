﻿
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Contract.IService;

public interface IOtpUserManager<TUserKey, TUser> : IUserManager<TUserKey, TUser>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
{
    Task<TUserKey> RequestLoginUser(long phonenumber, byte role);
    Task<TUserKey> RequestLoginUser(string email, byte role);
    Task<TUserKey> RequestRegisterUser(long phonenumber, string firstname, string lastName, byte role);
    Task<TUserKey> RequestRegisterUser(string email, string firstname, string lastName, byte role);
    Task<bool> VerifyRegisterOtpCode(string code, TUserKey id, byte roleId);
    Task<string> VerifyLoginOtpCode(string code, TUserKey id, byte roleId);
}