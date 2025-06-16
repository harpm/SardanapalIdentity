
using Sardanapal.Contract.IRepository;
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Contract.IRepository;

public interface IUserRepository<TUserKey, TUserModel>
    : ICrudRepository<TUserKey, TUserModel>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUserModel : class, IUser<TUserKey>, new()
{
}
