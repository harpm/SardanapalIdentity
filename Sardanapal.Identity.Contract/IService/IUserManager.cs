

using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.ViewModel.Models.Account;

namespace Sardanapal.Identity.Contract.IService;

public interface IUserManager<TUserKey, TUser, TRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRegisterVM : RegisterVM, new()
{
    Task<IResponse<TUser>> GetUser(string? email = null, long? phoneNumber = null);
    Task<IResponse<bool>> HasRole(TUserKey userKey, byte roleId);
    Task<IResponse<string>> Login(string username, string password);
    Task<IResponse<TUserKey>> RegisterUser(TRegisterVM model, byte roleId);
    Task<IResponse<string>> RefreshToken(TUserKey userId);
}
