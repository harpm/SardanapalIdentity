
using Microsoft.EntityFrameworkCore;
using Sardanapal.Ef.Repository;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;

namespace Sardanapal.Identity.Repository;

public abstract class UserRepositoryBase<TContext, TUserKey, TUserModel>
    : EFRepositoryBase<TContext, TUserKey, TUserModel>, IUserRepository<TUserKey, TUserModel>
    where TContext : DbContext
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey> 
    where TUserModel : class, IUser<TUserKey>, new()
{
    protected UserRepositoryBase(TContext context)
        : base(context)
    {
        
    }
}
