namespace Sardanapal.Identity.Share.Options;

public class IdentityInfo
{
    public string? SecretKey { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public int ExpirationTime { get; set; }
}

