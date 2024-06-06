
namespace Sardanapal.Identity.Services.Services.RoleManager;

public interface RoleManager<TUserKey, TRoleKey, TRole, TUR>
{
    TRole GetRole(TRoleKey roleId);
    bool HasRole(TRoleKey roleId, TUserKey userId);
}
