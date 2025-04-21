
namespace Sardanapal.Identity.Share.Static;

public class SdClaimTypes
{
    #region Base Claim Keys

    //
    // Summary:
    //     The URI for a claim that specifies the name of an entity, http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier.
    public const string NameIdentifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
    //
    // Summary:
    //     The URI for a claim that specifies the role of an entity, http://schemas.microsoft.com/ws/2008/06/identity/claims/role.
    public const string Role = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    //
    // Summary:
    //     The URI for a claim that specifies the anonymous user; http://schemas.xmlsoap.org/ws/2005/05/identity/claims/anonymous.
    public const string Anonymous = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/anonymous";
    //
    // Summary:
    //     http://schemas.microsoft.com/ws/2008/06/identity/claims/expiration.
    public const string Expiration = "http://schemas.microsoft.com/ws/2008/06/identity/claims/expiration";
    //
    // Summary:
    //     http://schemas.microsoft.com/ws/2008/06/identity/claims/expired.
    public const string Expired = "http://schemas.microsoft.com/ws/2008/06/identity/claims/expired";

    #endregion

    #region Sardanapal Claims

    /// <summary>
    /// Summary:
    ///     This Claims that specifically each user has access to exact parts of the software
    /// </summary>
    public const string AccessRights = "AccessRight";

    #endregion
}
