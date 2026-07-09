namespace Sardanapal.Identity.Contract.IModel;

public interface IControllerActionClaim<TKey> : IClaim<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    Guid ControllerId { get; set; }
    byte ActionType { get; set; }
}
