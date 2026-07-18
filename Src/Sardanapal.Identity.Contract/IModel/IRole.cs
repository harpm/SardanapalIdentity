using Sardanapal.Contract.IModel;

namespace Sardanapal.Identity.Contract.IModel;

public interface IRoleBase<TKey> : IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    string Title { get; set; }
}

public interface IRole<TKey, TUserKey, TUR> : IRoleBase<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TUR : IUserRole<TUserKey, TKey>
{
    public abstract ICollection<TUR> UserRoles { get; set; }
}
