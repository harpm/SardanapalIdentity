
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Domain.Model;

namespace Sardanapal.Identity.Services.Services.RoleManager;

public abstract class EFRoleManagerBase<TRepository, TUserKey, TRoleKey, TRole, TUR>
    : IRoleManager<TUserKey, TRoleKey, TRole, TUR>
    where TRepository : IEFRoleRepository<TRoleKey, TRole>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
    where TRole : class, IRole<TRoleKey, TUserKey, TUR>, new()
    where TUR : UserRoleBase<TUserKey, TRoleKey>, new()
{
    protected readonly TRepository _roleRepository;
    protected readonly ILogger _logger;

    protected EFRoleManagerBase(TRepository roleRepository, ILogger logger)
    {
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public virtual TRole GetRole(TRoleKey roleId)
    {
        return this._roleRepository.FetchById(roleId);
    }

    public Task<TRole> GetRoleAsync(TRoleKey roleId)
    {
        return this._roleRepository.FetchByIdAsync(roleId);
    }

    public virtual bool HasRole(TRoleKey roleId, TUserKey userId)
    {
        return this._roleRepository.FetchAll()
            .Where(x => x.Id.Equals(roleId) && x.UserRoles.Where(w => w.UserId.Equals(userId)).Any())
            .Any();
    }

    public Task<bool> HasRoleAsync(TRoleKey roleId, TUserKey userId)
    {
        return this._roleRepository.FetchAll()
            .Where(x => x.Id.Equals(roleId) && x.UserRoles.Where(w => w.UserId.Equals(userId)).Any())
            .AnyAsync();
    }
}
