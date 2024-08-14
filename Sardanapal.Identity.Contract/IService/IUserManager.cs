
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Contract.IService;

public interface IUserManagerService<TUserKey, TUser, TRole>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUser : class, IUser<TUserKey>, new()
    where TRole : class, IRole<byte>, new()
{
    Task<TUser?> GetUser(string? email = null, long? phoneNumber = null);
    Task<string> Login(string username, string password);
    Task<TUserKey> RegisterUser(string username, string password, byte role);

    void EditUserData(TUserKey id, string? username = null
        , string? password = null
        , long? phonenumber = null
        , string? email = null
        , string? firstname = null
        , string? lastname = null);
    Task<string> RefreshToken(TUserKey userId);
}
