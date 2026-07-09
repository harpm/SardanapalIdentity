namespace Sardanapal.Identity.Share.Static;

public class SdClaimTypes
{
    #region Base Claim Keys

    public const string NameIdentifier = "id";
    public const string Roles = "sd_roles";
    public const string MustChangePassword = "must_change_pw";

    #endregion

    #region Sardanapal Claims

    /// <summary>
    /// Summary:
    ///     This Claims that specifically each user has access to exact parts of the software
    /// </summary>
    public const string AccessRights = "AccessRight";

    /// <summary>
    ///     Claims that bind a user to a specific controller + action type.
    /// </summary>
    public const string ControllerAction = "sd_controller_action";

    #endregion
}
