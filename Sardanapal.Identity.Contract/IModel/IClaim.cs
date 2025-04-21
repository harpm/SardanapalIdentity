using Sardanapal.Contract.IModel;

namespace Sardanapal.Identity.Contract.IModel;

public interface IClaim<TKey> : IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    Guid ControllerId { get; set; }
    byte ActionType { get; set; }
}
