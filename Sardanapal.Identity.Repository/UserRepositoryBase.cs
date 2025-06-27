
using Microsoft.EntityFrameworkCore;
using Sardanapal.Ef.Repository;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;

namespace Sardanapal.Identity.Repository;

public abstract class UserRepositoryBase<TContext, TUserKey, TRoleKey, TUserModel, TUR>
    : EFRepositoryBase<TContext, TUserKey, TUserModel>, IUserRepository<TUserKey, TRoleKey, TUserModel, TUR>
    where TContext : DbContext
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey> 
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey> 
    where TUserModel : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, TRoleKey>, new()
{
    protected UserRepositoryBase(TContext context)
        : base(context)
    {
        
    }

    public IEnumerable<TUR> FetchAllUserRoles()
    {
        return _unitOfWork.Set<TUR>();
    }

    public Task<IEnumerable<TUR>> FetchAllUserRolesAsync()
    {
        return Task.FromResult(_unitOfWork.Set<TUR>().AsEnumerable());
    }


    public long AddUserRole(TUR userRole)
    {
        EnsureNotNullReference(userRole);
        _unitOfWork.Add(userRole);
        return userRole.Id;
    }

    public async Task<long> AddUserRoleAsync(TUR userRole)
    {
        EnsureNotNullReference(userRole);
        await _unitOfWork.AddAsync(userRole);
        return userRole.Id;
    }
}
