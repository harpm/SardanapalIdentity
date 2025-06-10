using Sardanapal.Identity.ViewModel.Models.Account;
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.Dto;

namespace Sardanapal.Identity.Contract.IService;

public interface IAccountService<TUserKey, TLoginVM, TLoginDto, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TLoginVM : LoginVM
    where TLoginDto : LoginDto
    where TRegisterVM : RegisterVM
{
    Task<IResponse<TLoginDto>> Login(LoginVM model);
    Task<IResponse<TUserKey>> Register(TRegisterVM model);
    Task<IResponse<string>> RefreshToken(TUserKey userId);

}
