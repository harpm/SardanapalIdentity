using System.Security.Claims;

namespace Sardanapal.Identity.Contract.IService;

public interface IIdentityProvider
{
    ClaimsPrincipal Claims { get; }
    bool IsAnanymous { get; }
    bool IsAuthorized { get; }
    string Token { get; }
    void SetAnanymous();
    void SetUserId(object userId);
    void SetToken(string token);
    void SetAuthorize();
    public void SetAuthorize(string token, ClaimsPrincipal claims);
    public void SetAuthorize(string token, ClaimsPrincipal claims, object userId);
}
