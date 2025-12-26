

using Sardanapal.Contract.IService;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Contract.IService;

public interface IUserManager<TUserKey, TUser, TSearchVM, TUserVM, TRegisterVM, TUserEditableVM>
    : IPanelService<TUserKey, TSearchVM, TUserVM, TRegisterVM, TUserEditableVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRegisterVM : RegisterVM<byte>, new()
    where TUserEditableVM : UserEditableVM, new()
    where TUserVM : UserVM<TUserKey>, new()
    where TSearchVM : UserSearchVM, new()
{
    Task<IResponse<TUser>> GetUser(TUserKey id);
    Task<IResponse<TUser>> GetUser(string username);
    Task<IResponse<string>> Login(TUserKey id);
    Task<IResponse<string>> RefreshToken(TUserKey userId);
    Task<IResponse<TUserKey>> RegisterUser(TRegisterVM model);
    Task<IResponse> Edit(TUserKey id, TUserEditableVM model);
    Task<IResponse> ChangePassword(TUserKey userId, string newPassword);
    Task<IResponse> VerifyUser(string recepient);
    Task<IResponse> DeleteUser(TUserKey userId);
}
