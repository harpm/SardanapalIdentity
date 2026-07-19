using FluentAssertions;
using Sardanapal.Identity.OTP.Services;
using Xunit;

namespace Sardanapal.Identity.OTP.Service.Tests.Unit;

public class OtpHelperTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(-7)]
    public void Constructor_Clamps_Length_Below_4_To_4(int requested)
    {
        var helper = new OtpHelper(requested);

        helper.OtpLength.Should().Be(4);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(50)]
    [InlineData(1000)]
    public void Constructor_Clamps_Length_Above_10_To_10(int requested)
    {
        var helper = new OtpHelper(requested);

        helper.OtpLength.Should().Be(10);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(10)]
    public void Constructor_Keeps_Length_Within_Range(int requested)
    {
        var helper = new OtpHelper(requested);

        helper.OtpLength.Should().Be(requested);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(10)]
    public void GenerateNewOtp_Returns_String_Of_Length_OtpLength(int length)
    {
        var helper = new OtpHelper(length);

        string code = helper.GenerateNewOtp();

        code.Should().HaveLength(length);
    }

    [Fact]
    public void GenerateNewOtp_Returns_Only_Digits_0_To_9()
    {
        var helper = new OtpHelper(6);

        for (int i = 0; i < 100; i++)
        {
            string code = helper.GenerateNewOtp();

            foreach (char c in code)
            {
                (c >= '0' && c <= '9').Should().BeTrue($"character '{c}' is not a decimal digit");
            }
        }
    }

    [Fact]
    public void GenerateNewOtp_Is_NonDeterministic_Across_Many_Calls()
    {
        var helper = new OtpHelper(8);
        var codes = new HashSet<string>();

        for (int i = 0; i < 1000; i++)
        {
            codes.Add(helper.GenerateNewOtp());
        }

        codes.Should().HaveCountGreaterThan(1, "many calls should not all return the same code");
    }

    [Fact]
    public void GenerateNewOtp_Distribution_Is_Reasonably_Uniform()
    {
        const int length = 4;
        const int samples = 100_000;
        var helper = new OtpHelper(length);

        int[] firstDigitCount = new int[10];
        for (int i = 0; i < samples; i++)
        {
            string code = helper.GenerateNewOtp();
            firstDigitCount[code[0] - '0']++;
        }

        int expectedPerDigit = samples / 10;
        int tolerance = (int)(expectedPerDigit * 0.05); // ±5% of uniform

        for (int digit = 0; digit < 10; digit++)
        {
            firstDigitCount[digit].Should().BeInRange(
                expectedPerDigit - tolerance,
                expectedPerDigit + tolerance,
                $"digit {digit} should appear roughly {expectedPerDigit} times (±5%)");
        }
    }
}
