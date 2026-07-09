using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Sardanapal.Ef.Repository;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Service.Repository;

namespace Sardanapal.Identity.Repository;

public abstract class EFUserRepositoryBase<TContext, TUserKey, TRoleKey, TUserModel, TUR, TUC, TClaim>
    : EFRepositoryBase<TContext, TUserKey, TUserModel>, IEFUserRepository<TUserKey, TRoleKey, TUserModel, TUR, TUC, TClaim>
    where TContext : DbContext
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey> 
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey> 
    where TUserModel : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, TRoleKey>, new()
    where TUC : class, IUserClaim<TUserKey, TRoleKey>, new()
    where TClaim : class, IClaim<TRoleKey>, new()
{
    protected EFUserRepositoryBase(TContext context)
        : base(context)
    {
        
    }

    public virtual IQueryable<TUR> FetchAllUserRoles()
    {
        return _unitOfWork.Set<TUR>();
    }

    public virtual Task<IQueryable<TUR>> FetchAllUserRolesAsync()
    {
        return Task.FromResult(_unitOfWork.Set<TUR>().AsQueryable());
    }


    public virtual long AddUserRole(TUR userRole)
    {
        EnsureNotNullReference(userRole);
        _unitOfWork.Add(userRole);
        return userRole.Id;
    }

    public virtual async Task<long> AddUserRoleAsync(TUR userRole)
    {
        EnsureNotNullReference(userRole);
        await _unitOfWork.AddAsync(userRole);
        return userRole.Id;
    }

    public virtual async Task<bool> DeleteUserRoleAsync(long urId)
    {
        EnsureNotNullReference(urId);
        var userRole = await _unitOfWork.Set<TUR>().FindAsync(urId);
        if (userRole != null)
        {
            _unitOfWork.Remove(userRole);
            return true;
        }
        return false;
    }

    public virtual async Task<bool> DeleteUserRoleAsync(TUserKey userId, TRoleKey urId)
    {
        EnsureNotNullReference(urId);
        var userRole = await _unitOfWork.Set<TUR>().Where(ur => ur.UserId.Equals(userId) && ur.Id.Equals(urId))
            .FirstOrDefaultAsync();
        if (userRole != null)
        {
            _unitOfWork.Remove(userRole);
            return true;
        }
        return false;
    }

    public virtual async Task<bool> DeleteUserRolesAsync(TUserKey userId, TRoleKey[] urId)
    {
        EnsureNotNullReference(urId);
        var userRoles = await _unitOfWork.Set<TUR>().Where(ur => ur.UserId.Equals(userId) && ur.Id.Equals(urId))
            .ToListAsync();
        if (userRoles != null && userRoles.Any())
        {
            _unitOfWork.RemoveRange(userRoles);
            return true;
        }
        return false;
    }

    public virtual IQueryable<TUC> FetchAllUserClaims()
    {
        return _unitOfWork.Set<TUC>();
    }

    public virtual Task<IQueryable<TUC>> FetchAllUserClaimsAsync()
    {
        return Task.FromResult(_unitOfWork.Set<TUC>().AsQueryable());
    }

    public virtual IQueryable<TClaim> FetchUserClaims(TUserKey userId)
    {
        return (from uc in _unitOfWork.Set<TUC>()
                where uc.UserId.Equals(userId)
                join c in _unitOfWork.Set<TClaim>() on uc.ClaimId equals c.Id
                select c);
    }

    public virtual long AddUserClaim(TUC userClaim)
    {
        EnsureNotNullReference(userClaim);
        _unitOfWork.Add(userClaim);
        return userClaim.Id;
    }

    public virtual async Task<long> AddUserClaimAsync(TUC userClaim)
    {
        EnsureNotNullReference(userClaim);
        await _unitOfWork.AddAsync(userClaim);
        return userClaim.Id;
    }

    public virtual async Task<bool> DeleteUserClaimsAsync(TUserKey userId, TRoleKey[] claimIds)
    {
        EnsureNotNullReference(claimIds);
        var userClaims = await _unitOfWork.Set<TUC>()
            .Where(uc => uc.UserId.Equals(userId) && claimIds.Contains(uc.ClaimId))
            .ToListAsync();
        if (userClaims != null && userClaims.Any())
        {
            _unitOfWork.RemoveRange(userClaims);
            return true;
        }
        return false;
    }
}

public abstract class UserRepositoryBase<TUserKey, TRoleKey, TUserModel, TUR, TUC, TClaim>
    : MemoryRepositoryBase<TUserKey, TUserModel>, IUserRepository<TUserKey, TRoleKey, TUserModel, TUR, TUC, TClaim>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TUserModel : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, TRoleKey>, new()
    where TUC : class, IUserClaim<TUserKey, TRoleKey>, new()
    where TClaim : class, IClaim<TRoleKey>, new()
{
    protected virtual ConcurrentDictionary<long, TUR> _turDB { get; set; } = new ConcurrentDictionary<long, TUR>();
    protected virtual ConcurrentDictionary<long, TUC> _tucDB { get; set; } = new ConcurrentDictionary<long, TUC>();
    protected virtual ConcurrentDictionary<TRoleKey, TClaim> _claimDB { get; set; } = new ConcurrentDictionary<TRoleKey, TClaim>();

    protected UserRepositoryBase()
        : base()
    {

    }

    public virtual IEnumerable<TUR> FetchAllUserRoles()
    {
        return _turDB.Values;
    }

    public virtual Task<IEnumerable<TUR>> FetchAllUserRolesAsync()
    {
        return Task.FromResult(_turDB.Values.AsEnumerable());
    }


    public virtual long AddUserRole(TUR userRole)
    {
        //EnsureNotNullReference(userRole);
        userRole.Id = _turDB.Count > 0 ? _turDB.Keys.Max() + 1 : 0;
        var addedUserRole = _turDB.GetOrAdd(userRole.Id, userRole);
        return addedUserRole.Id;
    }

    public virtual async Task<long> AddUserRoleAsync(TUR userRole)
    {
        //EnsureNotNullReference(userRole);
        userRole.Id = _turDB.Count > 0 ? _turDB.Keys.Max() + 1 : 0;
        var addedUserRole = await Task.FromResult(_turDB.GetOrAdd(userRole.Id, userRole));
        return addedUserRole.Id;
    }

    public virtual Task<bool> DeleteUserRolesAsync(TUserKey userId, TRoleKey[] roleIds)
    {
        var toRemove = _turDB.Values
            .Where(ur => ur.UserId.Equals(userId) && roleIds.Contains(ur.RoleId))
            .Select(ur => ur.Id)
            .ToList();

        bool allRemoved = true;
        foreach (var id in toRemove)
        {
            if (!_turDB.TryRemove(id, out _))
                allRemoved = false;
        }

        return Task.FromResult(allRemoved && toRemove.Count > 0);
    }

    public virtual IEnumerable<TUC> FetchAllUserClaims()
    {
        return _tucDB.Values;
    }

    public virtual Task<IEnumerable<TUC>> FetchAllUserClaimsAsync()
    {
        return Task.FromResult(_tucDB.Values.AsEnumerable());
    }

    public virtual IEnumerable<TClaim> FetchUserClaims(TUserKey userId)
    {
        return (from uc in _tucDB.Values
                where uc.UserId.Equals(userId)
                join c in _claimDB.Values on uc.ClaimId equals c.Id
                select c);
    }

    public virtual long AddUserClaim(TUC userClaim)
    {
        userClaim.Id = _tucDB.Count > 0 ? _tucDB.Keys.Max() + 1 : 0;
        var addedUserClaim = _tucDB.GetOrAdd(userClaim.Id, userClaim);
        return addedUserClaim.Id;
    }

    public virtual async Task<long> AddUserClaimAsync(TUC userClaim)
    {
        userClaim.Id = _tucDB.Count > 0 ? _tucDB.Keys.Max() + 1 : 0;
        var addedUserClaim = await Task.FromResult(_tucDB.GetOrAdd(userClaim.Id, userClaim));
        return addedUserClaim.Id;
    }

    public virtual Task<bool> DeleteUserClaimsAsync(TUserKey userId, TRoleKey[] claimIds)
    {
        var toRemove = _tucDB.Values
            .Where(uc => uc.UserId.Equals(userId) && claimIds.Contains(uc.ClaimId))
            .Select(uc => uc.Id)
            .ToList();

        bool allRemoved = true;
        foreach (var id in toRemove)
        {
            if (!_tucDB.TryRemove(id, out _))
                allRemoved = false;
        }

        return Task.FromResult(allRemoved && toRemove.Count > 0);
    }

    public virtual void AddClaim(TClaim claim)
    {
        _claimDB.AddOrUpdate(claim.Id, claim, (_, _) => claim);
    }

    public virtual Task AddClaimAsync(TClaim claim)
    {
        AddClaim(claim);
        return Task.CompletedTask;
    }
}
