
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Static;
using Sardanapal.ViewModel.Response;
using System.Security.Claims;

namespace Sardanapal.Identity.Authorization.Data;

public class IdentityProvider : IIdentityProvider
{
    #region Services

    protected readonly ITokenService _tokenService;

    #endregion

    #region Properties

    protected string _userId;
    public string UserId
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
            return _authorized || _isAnanymous;
        }
    }

    protected bool _isAnanymous;
    public bool IsAnanymous
    {
        get
        {
            return _isAnanymous;
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

    #endregion

    public IdentityProvider(ITokenService tokenService)
    {
        this._tokenService = tokenService;
    }

    public virtual void SetAnanymous()
    {
        _isAnanymous = true;
    }

    public virtual void SetAuthorize()
    {
        _authorized = true;
    }

    public virtual void SetAuthorize(string token)
    {
        var res = _tokenService.ValidateToken(token, out ClaimsPrincipal cp);
        if (res.StatusCode == StatusCode.Succeeded && res.Data)
        {
            SetAuthorize();
            SetToken(token);
            _claims = cp;

            var id = cp?.FindFirst(SdClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(id))
            {
                SetUserId(id);
            }
        }
    }

    protected virtual void SetToken(string token)
    {
        _token = token;
    }

    protected virtual void SetUserId(object userId)
    {
        _userId = userId.ToString();
    }
}
