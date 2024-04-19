
using System.Security.Claims;

namespace Sardanapal.Identity.Authorization.Data;

public interface IIdentityHolder
{
    ClaimsPrincipal Principals { get; }
    bool IsAuthorized { get; }
    void SetUserId(string userId);
    void SetToken(string token);
    string GetToken();
    void SetAuthorize();
}
