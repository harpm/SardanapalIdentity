using Sardanapal.Contract.IRepository;
using Sardanapal.Ef.Repository;
using Sardanapal.Identity.Contract.IModel;

namespace Sardanapal.Identity.Contract.IRepository;

public interface IEFUserRepository<TUserKey, TRoleKey, TUserModel, TUR, TUC, TClaim>
    : IEFCrudRepository<TUserKey, TUserModel>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TUserModel : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, TRoleKey>, new()
    where TUC : class, IUserClaim<TUserKey, TRoleKey>, new()
    where TClaim : class, IClaim<TRoleKey>, new()
{
    IQueryable<TUR> FetchAllUserRoles();
    Task<IQueryable<TUR>> FetchAllUserRolesAsync();

    long AddUserRole(TUR userRole);
    Task<long> AddUserRoleAsync(TUR userRole);

    Task<bool> DeleteUserRoleAsync(TUserKey userId, TRoleKey uid);
    Task<bool> DeleteUserRolesAsync(TUserKey userId, TRoleKey[] uids);

    IQueryable<TUC> FetchAllUserClaims();
    Task<IQueryable<TUC>> FetchAllUserClaimsAsync();

    /// <summary>
    /// Aggregates the Claim entities assigned to a user by joining
    /// UserClaims -> Claims. Returns the claim entities (with their
    /// ClaimType discriminator and payload fields) - not the link rows.
    /// </summary>
    IQueryable<TClaim> FetchUserClaims(TUserKey userId);

    long AddUserClaim(TUC userClaim);
    Task<long> AddUserClaimAsync(TUC userClaim);

    Task<bool> DeleteUserClaimsAsync(TUserKey userId, TRoleKey[] claimIds);
}

public interface IUserRepository<TUserKey, TRoleKey, TUserModel, TUR, TUC, TClaim>
    : ICrudRepository<TUserKey, TUserModel>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TUserModel : class, IUser<TUserKey>, new()
    where TUR : class, IUserRole<TUserKey, TRoleKey>, new()
    where TUC : class, IUserClaim<TUserKey, TRoleKey>, new()
    where TClaim : class, IClaim<TRoleKey>, new()
{
    IEnumerable<TUR> FetchAllUserRoles();
    Task<IEnumerable<TUR>> FetchAllUserRolesAsync();

    long AddUserRole(TUR userRole);
    Task<long> AddUserRoleAsync(TUR userRole);

    Task<bool> DeleteUserRolesAsync(TUserKey userId, TRoleKey[] roleIds);

    IEnumerable<TUC> FetchAllUserClaims();
    Task<IEnumerable<TUC>> FetchAllUserClaimsAsync();

    /// <summary>
    /// Aggregates the Claim entities assigned to a user by joining
    /// UserClaims -> Claims.
    /// </summary>
    IEnumerable<TClaim> FetchUserClaims(TUserKey userId);

    long AddUserClaim(TUC userClaim);
    Task<long> AddUserClaimAsync(TUC userClaim);

    Task<bool> DeleteUserClaimsAsync(TUserKey userId, TRoleKey[] claimIds);

    void AddClaim(TClaim claim);
    Task AddClaimAsync(TClaim claim);
}
