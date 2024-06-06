
using System.Security.Claims;

namespace Sardanapal.Identity.Authorization.Data;

public interface IIdentityHolder
{
    ClaimsPrincipal Claims { get; }
    bool IsAuthorized { get; }
    string Token { get; }
    void SetUserId(object userId);
    void SetToken(string token);
    void SetAuthorize();
    public void SetAuthorize(string token, ClaimsPrincipal claims);
    public void SetAuthorize(string token, ClaimsPrincipal claims, object userId);
}

public class IdentityHolder : IIdentityHolder
{
    protected long _userId;
    public long UserId
    {
        get
        {
            return _userId;
        }
    }

    protected ClaimsPrincipal _claims;
    public ClaimsPrincipal Claims
    {
        get
        {
            return _claims;
        }
    }

    protected bool _authorized;
    public bool IsAuthorized
    {
        get
        {
            return _authorized;
        }
    }

    protected string _token;
    public string Token
    {
        get
        {
            return _token;
        }
    }

    public void SetAuthorize()
    {
        _authorized = true;
    }

    public void SetAuthorize(string token, ClaimsPrincipal claims)
    {
        _token = token;
        _claims = claims;
    }

    public void SetAuthorize(string token, ClaimsPrincipal claims, object userId)
    {
        _token = token;
        _claims = claims;
        _userId = Convert.ToInt64(userId.ToString());
    }

    public void SetToken(string token)
    {
        _token = token;
    }

    public void SetUserId(object userId)
    {
        _userId = Convert.ToInt64(userId.ToString());
    }
}