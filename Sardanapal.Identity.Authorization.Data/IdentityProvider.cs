﻿
using Sardanapal.Identity.Contract.IService;
using System.Security.Claims;

namespace Sardanapal.Identity.Authorization.Data;

public class IdentityProvider : IIdentityProvider
{
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

    public virtual void SetAnanymous()
    {
        _isAnanymous = true;
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
        _userId = userId.ToString();
    }
}