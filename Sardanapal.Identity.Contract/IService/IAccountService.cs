using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Contract.IService;

public interface IAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto, new()
    where TRegisterVM : RegisterVM, new()
{
    Task<IResponse<TLoginDto>> Login(TLoginVM model);
    Task<IResponse<TUserKey>> Register(TRegisterVM model);
    Task<IResponse> ChangePassword(ChangePasswordVM<TUserKey> model);
    Task<IResponse> ChangePassword(ChangePasswordVM model);
    Task<IResponse<string>> RefreshToken(TUserKey userId);

}
