using Sardanapal.Contract.IModel;

namespace Sardanapal.Identity.Contract.IModel;

public interface IClaim
{
    byte ClaimType { get; }
}

public interface IClaim<TKey> : IClaim, IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
}
