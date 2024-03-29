namespace Sardanapal.Identity.ViewModel.Models;

public class OtpRequestVM
{
    public string? Email { get; set; }
    public long? PhoneNumber { get; set; }
}

public class OtpLoginVM<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public string OtpCode { get; set; }
    public TUserKey UserId { get; set; }
    public byte RoleId { get; set; }
}
