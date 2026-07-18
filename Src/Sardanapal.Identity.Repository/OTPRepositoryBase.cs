
using Microsoft.EntityFrameworkCore;
using Sardanapal.Contract.IModel;
using Sardanapal.Ef.Repository;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Service.Repository;

namespace Sardanapal.Identity.Repository;

public abstract class EFOTPRepositoryBase<TContext, TKey, TModel>
    : EFRepositoryBase<TContext, TKey, TModel>, IEFOTPRepository<TKey, TModel>
    where TContext : DbContext
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TModel : class, IBaseEntityModel<TKey>, new()
{
    protected EFOTPRepositoryBase(TContext context)
        : base(context)
    {

    }

    public void RemoveRange(IEnumerable<TKey> keys)
    {
        if (keys == null || !keys.Any()) throw new ArgumentNullException(nameof(keys));

        var entries = _unitOfWork.Set<TModel>().Where(x => keys.Contains(x.Id)).ToArray();

        RemoveRange(entries);
    }

    public void RemoveRange(IEnumerable<TModel> entries)
    {
        if (entries == null || !entries.Any()) throw new KeyNotFoundException();

        _unitOfWork.RemoveRange(entries);
    }
}

public abstract class OTPRepositoryBase<TKey, TModel>
    : MemoryRepositoryBase<TKey, TModel>, IOTPRepository<TKey, TModel>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TModel : class, IBaseEntityModel<TKey>, new()
{
    protected OTPRepositoryBase()
        : base()
    {

    }

    public void RemoveRange(IEnumerable<TKey> keys)
    {
        if (keys == null || !keys.Any()) throw new ArgumentNullException(nameof(keys));

        if (keys.Where(x => !_db.Keys.Contains(x)).Any()) throw new KeyNotFoundException();

        keys.ToList().ForEach(key => _db.Keys.Remove(key));
    }

    public void RemoveRange(IEnumerable<TModel> entries)
    {
        if (entries == null || !entries.Any()) throw new KeyNotFoundException();

        RemoveRange(entries.Select(x => x.Id));
    }
}
