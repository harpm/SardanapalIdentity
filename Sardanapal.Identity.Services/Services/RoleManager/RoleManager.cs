
using Sardanapal.Identity.Contract.IService;

namespace Sardanapal.Identity.Services.Services.RoleManager;

public abstract class RoleManagerBase<TUserKey, TRoleKey, TRole, TUR>
    : IRoleManager<TUserKey, TRoleKey, TRole, TUR>
{
    public virtual TRole GetRole(TRoleKey roleId)
    {
        throw new NotImplementedException();
    }

    public virtual bool HasRole(TRoleKey roleId, TUserKey userId)
    {
        throw new NotImplementedException();
    }
}
