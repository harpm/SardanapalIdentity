
namespace Sardanapal.Identity.Contract.IService;

public interface IOtpHelper
{
    int OtpLength { get; }
    string GenerateNewOtp();
}
