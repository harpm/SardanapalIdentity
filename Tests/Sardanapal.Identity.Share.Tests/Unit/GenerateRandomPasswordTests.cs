using System.Text;
using FluentAssertions;
using Sardanapal.Identity.Share.Static;
using Xunit;

namespace Sardanapal.Identity.Share.Tests.Unit;

public class GenerateRandomPasswordTests
{
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private static readonly string Allowed = Lower + Upper + Digits;

    [Fact]
    public void GenerateRandomPassword_Default_Length_Is_16()
    {
        string password = Utilities.GenerateRandomPassword();

        password.Should().HaveLength(16);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(-5)]
    public void GenerateRandomPassword_Clamps_Length_Below_4_To_4(int length)
    {
        string password = Utilities.GenerateRandomPassword(length);

        password.Should().HaveLength(4);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    public void GenerateRandomPassword_Honors_Large_Length_No_Upper_Clamp(int length)
    {
        string password = Utilities.GenerateRandomPassword(length);

        password.Should().HaveLength(length);
    }

    [Fact]
    public void GenerateRandomPassword_Always_Contains_LowerUpper_And_Digit()
    {
        for (int i = 0; i < 100; i++)
        {
            string password = Utilities.GenerateRandomPassword();

            password.Any(Lower.Contains).Should().BeTrue("a lowercase letter is required");
            password.Any(Upper.Contains).Should().BeTrue("an uppercase letter is required");
            password.Any(Digits.Contains).Should().BeTrue("a digit is required");
        }
    }

    [Fact]
    public void GenerateRandomPassword_Contains_Only_Allowed_Charset()
    {
        for (int i = 0; i < 100; i++)
        {
            string password = Utilities.GenerateRandomPassword(32);

            foreach (char c in password)
            {
                Allowed.Contains(c).Should().BeTrue($"character '{c}' is outside the allowed charset");
            }
        }
    }

    [Fact]
    public void GenerateRandomPassword_Is_NonDeterministic_Across_Calls()
    {
        var passwords = new HashSet<string>();
        for (int i = 0; i < 10; i++)
        {
            passwords.Add(Utilities.GenerateRandomPassword());
        }

        passwords.Should().HaveCountGreaterThan(1, "repeated calls should not all produce the same value");
    }
}
