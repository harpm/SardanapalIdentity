
namespace Sardanapal.Identity.Share.Static;

/// <summary>
/// Well-known header keys and protocol constants used across the Sardanapal identity pipeline.
/// </summary>
public static class ConstantKeys
{
    /// <summary>
    /// The HTTP request/response header that carries the JWT.
    /// The header name is <c>"AUTH"</c> (not the standard <c>Authorization</c> header).
    /// The value must be the <b>raw JWT</b> with <b>no <c>Bearer </c> scheme prefix</b>.
    /// The refresh-token middleware reissues a fresh token on the same response header
    /// when the incoming token is near expiry.
    /// </summary>
    public const string AUTH_HEADER_KEY = "AUTH";
}
