
namespace Sardanapal.Identity.Share.Static;

public class SdClaimTypes
{
    #region Base Claim Keys

    public const string NameIdentifier = "id";
    public const string Roles = "roles";

    #endregion

    #region Sardanapal Claims

    /// <summary>
    /// Summary:
    ///     This Claims that specifically each user has access to exact parts of the software
    /// </summary>
    public const string AccessRights = "AccessRight";

    #endregion
}
