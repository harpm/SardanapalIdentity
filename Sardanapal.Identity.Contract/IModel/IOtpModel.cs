using Sardanapal.Contract.IModel;

namespace Sardanapal.Identity.Contract.IModel;

public interface IOTPModel<TUserKey, TKey>
    : IBaseEntityModel<TKey>, ICachModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    string Code { get; set; }

    TUserKey UserId { get; set; }

    DateTime ExpireTime { get; set; }

    byte RoleId { get; set; }
}
