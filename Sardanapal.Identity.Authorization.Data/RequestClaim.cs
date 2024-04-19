using System.Security.Claims;

namespace Sardanapal.Identity.Authorization.Data;

public class RequestClaim
{
    public ClaimsPrincipal Claims { get; set; }
}
