using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Contract.IService;

public interface IAccountService<TUserKey, TRegisterVM, TUserEditableVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRegisterVM : RegisterVM<byte>, new()
    where TUserEditableVM : UserEditableVM, new()
{
    Task<IResponse> Edit(TUserKey userId, TUserEditableVM model);
    Task<IResponse<TUserEditableVM>> GetEditable(TUserKey userId);
    Task<IResponse<LoginDto>> Login(LoginVM model);
    Task<IResponse<TUserKey>> Register(TRegisterVM model);
    Task<IResponse> ChangePassword(ChangePasswordVM<TUserKey> model);
    Task<IResponse> ChangePassword(ChangePasswordVM model);
    Task<IResponse<string>> RefreshToken(TUserKey userId);
    Task<IResponse> Delete(TUserKey userId);
}
