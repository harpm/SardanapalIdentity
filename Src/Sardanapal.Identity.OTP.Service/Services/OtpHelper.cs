using Sardanapal.Identity.Contract.IService;

namespace Sardanapal.Identity.OTP.Services;

public class OtpHelper : IOtpHelper
{
    private const int MinLength = 4;
    private const int MaxLength = 10;

    protected int _otpLength;
    public int OtpLength { get { return _otpLength; } }

    public OtpHelper(int otpLength)
    {
        if (otpLength < MinLength)
            _otpLength = MinLength;
        else if (otpLength > MaxLength)
            _otpLength = MaxLength;
        else
            _otpLength = otpLength;
    }

    public virtual string GenerateNewOtp()
    {
        Span<char> digits = stackalloc char[OtpLength];
        for (int i = 0; i < OtpLength; i++)
        {
            digits[i] = (char)('0' + Random.Shared.Next(0, 10));
        }
        return new string(digits);
    }
}
