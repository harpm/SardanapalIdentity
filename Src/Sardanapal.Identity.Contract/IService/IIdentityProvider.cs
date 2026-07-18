using System.Security.Claims;

namespace Sardanapal.Identity.Contract.IService;

public interface IIdentityProvider
{
    ClaimsPrincipal Claims { get; }
    bool IsAnonymous { get; }
    bool IsAuthorized { get; }
    string Token { get; }
    void SetAnonymous();
    void SetAuthorize();
    void SetAuthorize(string token);
}
