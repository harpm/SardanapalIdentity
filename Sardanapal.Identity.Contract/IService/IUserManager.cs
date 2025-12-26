

using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.ViewModel.Models.Account;

namespace Sardanapal.Identity.Contract.IService;

public interface IUserManager<TUserKey, TUser, TRegisterVM, TUserEditableVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRegisterVM : RegisterVM<byte>, new()
    where TUserEditableVM : UserEditableVM, new()
{
    Task<IResponse> Edit(TUserKey id, TUserEditableVM model);
    Task<IResponse<TUserEditableVM>> GetUserEditable(TUserKey id);
    Task<IResponse<TUser>> GetUser(string username);
    Task<IResponse<bool>> HasRole(TUserKey userKey, byte roleId);
    Task<IResponse<string>> Login(string username, string password);
    Task<IResponse<TUserKey>> RegisterUser(TRegisterVM model);
    Task<IResponse> ChangePassword(TUserKey userId, string newPassword);
    Task<IResponse<string>> RefreshToken(TUserKey userId);
    Task<IResponse> DeleteUser(TUserKey userId);
}
