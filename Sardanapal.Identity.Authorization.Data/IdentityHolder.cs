
using System.Security.Claims;

namespace Sardanapal.Identity.Authorization.Data;

public class IdentityHolder : IIdentityProvider
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

    public virtual void SetAuthorize()
    {
        _authorized = true;
    }

    public virtual void SetAuthorize(string token, ClaimsPrincipal claims)
    {
        SetAuthorize();
        SetToken(token);
        _claims = claims;
    }

    public virtual void SetAuthorize(string token, ClaimsPrincipal claims, object userId)
    {
        SetAuthorize(token, claims);
        SetUserId(userId);
    }

    public virtual void SetToken(string token)
    {
        _token = token;
    }

    public virtual void SetUserId(object userId)
    {
        _userId = Convert.ToInt64(userId.ToString());
    }
}