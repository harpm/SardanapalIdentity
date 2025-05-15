
using Microsoft.EntityFrameworkCore;
using Sardanapal.Contract.IModel;
using Sardanapal.Ef.Repository;

namespace Sardanapal.Identity.Repository;

public abstract class ClaimRepositoryBase<TContext, TKey, TModel>
    : EFRepositoryBase<TContext, TKey, TModel>
    where TContext : DbContext
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TModel : class, IBaseEntityModel<TKey>, new()
{
    protected ClaimRepositoryBase(TContext context)
        : base(context)
    {
        
    }
}
