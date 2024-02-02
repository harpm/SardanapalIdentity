namespace Sardanapal.Identity.OTP.Services;

public class OtpService
{
    protected int OtpLenght { get; set; }
    
    public OtpService(int otpLength)
    {
        OtpLenght = otpLength;
    }

    public string GenerateNewOtp()
    {
        char[] nines = new char[OtpLenght];
        Array.Fill(nines, '9');
        return Random.Shared.Next(Convert.ToInt32(string.Join("", nines)))
            .ToString("D" + OtpLenght);
    }
}

