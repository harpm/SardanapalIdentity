
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Contract.IService;

public interface IOtpUserManager<TUserKey, TUser, TRegisterVM, TOTPRequestRegisterVM, TUserEditable>
    : IUserManager<TUserKey, TUser, TRegisterVM, TUserEditable>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRegisterVM : RegisterVM<byte>, new()
    where TOTPRequestRegisterVM : OtpRegisterRequestVM, new()
    where TUserEditable : UserEditableVM, new()
{
    Task<IResponse<TUserKey>> RequestLoginUser(ulong phonenumber, byte role);
    Task<IResponse<TUserKey>> RequestLoginUser(string email, byte role);
    Task<IResponse<TUserKey>> RequestRegisterUser(TOTPRequestRegisterVM model, byte role);
    Task<IResponse> VerifyRegisterOtpCode(string code, TUserKey id, byte roleId);
    Task<IResponse<string>> VerifyLoginOtpCode(string code, TUserKey id, byte roleId);
    Task<IResponse<TUserKey>> RequestResetPassword(ulong phonenumber);
    Task<IResponse<TUserKey>> RequestResetPassword(string email);
    Task<IResponse> ResetPassword(string code, TUserKey id, byte roleId, string newPassword);
}
