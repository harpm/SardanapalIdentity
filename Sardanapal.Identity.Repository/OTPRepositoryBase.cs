
using Microsoft.EntityFrameworkCore;
using Sardanapal.Contract.IModel;
using Sardanapal.Ef.Repository;
using Sardanapal.Identity.Contract.IRepository;

namespace Sardanapal.Identity.Repository;

public abstract class OTPRepositoryBase<TContext, TKey, TModel>
    : EFRepositoryBase<TContext, TKey, TModel>, IOTPRepository<TKey, TModel>
    where TContext : DbContext
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TModel : class, IBaseEntityModel<TKey>, new()
{
    protected OTPRepositoryBase(TContext context)
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
