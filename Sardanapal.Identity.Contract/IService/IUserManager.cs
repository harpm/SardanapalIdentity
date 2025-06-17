
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Contract.IService;

public interface IUserManager<TUserKey, TUser, TRole, TClaim>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRole<byte>, new()
    where TClaim : class, IClaim<byte>, new()
{
    Task<IResponse<TUser>> GetUser(string? email = null, long? phoneNumber = null);
    Task<IResponse<string>> Login(string username, string password);
    Task<IResponse<TUserKey>> RegisterUser(string username, string password, byte role);

    Task<IResponse> EditUserData(TUserKey id, string? username = null
        , string? password = null
        , long? phonenumber = null
        , string? email = null
        , string? firstname = null
        , string? lastname = null);
    Task<IResponse<string>> RefreshToken(TUserKey userId);
}
