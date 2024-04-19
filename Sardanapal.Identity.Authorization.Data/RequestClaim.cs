using System.Security.Claims;

namespace Sardanapal.Identity.Authorization.Data;

public interface IRequestClaim<TUserKey>
    where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
{
    ClaimsPrincipal Claims { protected get; set; }
    TUserKey GetCurrentUserId();
}
