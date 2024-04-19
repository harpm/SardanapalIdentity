
namespace Sardanapal.Identity.Authorization.Data;

public interface IIdentityHolder
{
    bool IsAuthorized { get; }

    void SetUserId(string userId);
    void SetToken(string token);
    string GetToken();
    void SetAuthorize();
}
