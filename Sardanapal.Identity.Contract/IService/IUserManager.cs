
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Contract.IService;

public interface IUserManager<TUserKey, TUser>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
{
    Task<IResponse<TUser>> GetUser(string? email = null, long? phoneNumber = null);
    Task<IResponse<string>> Login(string username, string password);
    Task<IResponse<TUserKey>> RegisterUser(string username, string password, byte role);
    Task<IResponse<string>> RefreshToken(TUserKey userId);
}
