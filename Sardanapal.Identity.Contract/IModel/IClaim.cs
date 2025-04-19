using Sardanapal.Contract.IModel;
using Sardanapal.Identity.Share.Types;

namespace Sardanapal.Identity.Contract.IModel;

public interface IClaim<TKey> : IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    string Boundery { get; set; }
    ClaimActionTypes ActionType { get; set; }
}
