using System.Security.Claims;

namespace Sardanapal.Identity.Contract.IService;

public interface IIdentityProvider
{
    ClaimsPrincipal Claims { get; }
    bool IsAnanymous { get; }
    bool IsAuthorized { get; }
    string Token { get; }
    void SetAnanymous();
    void SetAuthorize();
    void SetAuthorize(string token);
}
