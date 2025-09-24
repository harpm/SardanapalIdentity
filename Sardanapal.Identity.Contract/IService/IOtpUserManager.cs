
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Contract.IService;

public interface IOtpUserManager<TUserKey, TUser> : IUserManager<TUserKey, TUser>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
{
    Task<IResponse<TUserKey>> RequestLoginUser(long phonenumber, byte role);
    Task<IResponse<TUserKey>> RequestLoginUser(string email, byte role);
    Task<IResponse<TUserKey>> RequestRegisterUser(long phonenumber, string firstname, string lastName, byte role);
    Task<IResponse<TUserKey>> RequestRegisterUser(string email, string firstname, string lastName, byte role);
    Task<IResponse> VerifyRegisterOtpCode(string code, TUserKey id, byte roleId);
    Task<IResponse<string>> VerifyLoginOtpCode(string code, TUserKey id, byte roleId);
}
