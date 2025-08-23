
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Sardanapal.Ef.Repository;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Service.Repository;

namespace Sardanapal.Identity.Repository;

public abstract class EFUserRepositoryBase<TContext, TUserKey, TRoleKey, TUserModel, TUR>
    : EFRepositoryBase<TContext, TUserKey, TUserModel>, IEFUserRepository<TUserKey, TRoleKey, TUserModel, TUR>
    where TContext : DbContext
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey> 
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey> 
    where TUserModel : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, TRoleKey>, new()
{
    protected EFUserRepositoryBase(TContext context)
        : base(context)
    {
        
    }

    public IQueryable<TUR> FetchAllUserRoles()
    {
        return _unitOfWork.Set<TUR>();
    }

    public Task<IQueryable<TUR>> FetchAllUserRolesAsync()
    {
        return Task.FromResult(_unitOfWork.Set<TUR>().AsQueryable());
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

public abstract class UserRepositoryBase<TUserKey, TRoleKey, TUserModel, TUR>
    : MemoryRepositoryBase<TUserKey, TUserModel>, IUserRepository<TUserKey, TRoleKey, TUserModel, TUR>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TUserModel : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, TRoleKey>, new()
{
    protected virtual ConcurrentDictionary<long, TUR> _turDB { get; set; } = new ConcurrentDictionary<long, TUR>();

    protected UserRepositoryBase()
        : base()
    {

    }

    public IEnumerable<TUR> FetchAllUserRoles()
    {
        return _turDB.Values;
    }

    public Task<IEnumerable<TUR>> FetchAllUserRolesAsync()
    {
        return Task.FromResult(_turDB.Values.AsEnumerable());
    }


    public long AddUserRole(TUR userRole)
    {
        //EnsureNotNullReference(userRole);
        userRole.Id = _turDB.Count > 0 ? _turDB.Keys.Max() + 1 : 0;
        var addedUserRole = _turDB.GetOrAdd(userRole.Id, userRole);
        return addedUserRole.Id;
    }

    public async Task<long> AddUserRoleAsync(TUR userRole)
    {
        //EnsureNotNullReference(userRole);
        userRole.Id = _turDB.Count > 0 ? _turDB.Keys.Max() + 1 : 0;
        var addedUserRole = await Task.FromResult(_turDB.GetOrAdd(userRole.Id, userRole));
        return addedUserRole.Id;
    }
}
