
namespace Sardanapal.Identity.Contract.IService;

public interface IRoleManager<TUserKey, TRoleKey, TRole, TUR>
{
    TRole GetRole(TRoleKey roleId);
    bool HasRole(TRoleKey roleId, TUserKey userId);
}
