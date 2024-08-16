
using Sardanapal.Identity.Contract.IService;

namespace Sardanapal.Identity.Services.Services.RoleManager;

public abstract class RoleManagerBase<TUserKey, TRoleKey, TRole, TUR>
    : IRoleManager<TUserKey, TRoleKey, TRole, TUR>
{
    public TRole GetRole(TRoleKey roleId)
    {
        throw new NotImplementedException();
    }

    public bool HasRole(TRoleKey roleId, TUserKey userId)
    {
        throw new NotImplementedException();
    }
}
