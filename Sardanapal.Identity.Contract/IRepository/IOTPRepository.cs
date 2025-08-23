
using Sardanapal.Contract.IModel;
using Sardanapal.Contract.IRepository;

namespace Sardanapal.Identity.Contract.IRepository;

public interface IOTPRepository<TKey, TModel>
    : ICrudRepository<TKey, TModel>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TModel : class, IBaseEntityModel<TKey>, new()
{
    void RemoveRange(IEnumerable<TKey> key);
    void RemoveRange(IEnumerable<TModel> entries);
}

public interface IEFOTPRepository<TKey, TModel>
    : IEFCrudRepository<TKey, TModel>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TModel : class, IBaseEntityModel<TKey>, new()
{
    void RemoveRange(IEnumerable<TKey> key);
    void RemoveRange(IEnumerable<TModel> entries);
}
