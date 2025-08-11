using Sardanapal.Identity.Contract.IService;

namespace Sardanapal.Identity.OTP.Services;

public class OtpHelper : IOtpHelper
{
    protected int _otpLength;
    public int OtpLength { get { return _otpLength; } }

    public OtpHelper(int otpLength)
    {
        _otpLength = otpLength;
    }

    public virtual string GenerateNewOtp()
    {
        char[] nines = new char[OtpLength];
        Array.Fill(nines, '9');
        return Random.Shared.Next(Convert.ToInt32(string.Join("", nines)))
            .ToString("D" + OtpLength);
    }
}
