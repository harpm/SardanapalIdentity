
namespace Sardanapal.Identity.Services.Services.RoleManager;

public interface IRoleManager<TUserKey, TRoleKey, TRole, TUR>
{
    TRole GetRole(TRoleKey roleId);
    bool HasRole(TRoleKey roleId, TUserKey userId);
}
