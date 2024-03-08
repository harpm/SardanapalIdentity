namespace Sardanapal.Identity.ViewModel.Models;

public class OtpRequestVM
{
    public string? Email { get; set; }
    public long? PhoneNumber { get; set; }
}

public class OtpLoginVM
{
    public string OtpCode { get; set; }
}
