using FluentAssertions;
using Sardanapal.Identity.Share.Static;
using Xunit;

namespace Sardanapal.Identity.Share.Tests.Unit;

public class VerifyPasswordTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void VerifyPassword_Returns_False_For_Null_Or_Empty_Password(string? password)
    {
        string storedHash = Utilities.HashPassword("some-password");

        bool result = Utilities.VerifyPassword(password!, storedHash);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void VerifyPassword_Returns_False_For_Null_Or_Empty_StoredHash(string? storedHash)
    {
        bool result = Utilities.VerifyPassword("some-password", storedHash!);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("onepart")]
    [InlineData("two.parts")]
    [InlineData("four.parts.here.extra")]
    public void VerifyPassword_Returns_False_For_Malformed_Hash_Wrong_Part_Count(string storedHash)
    {
        bool result = Utilities.VerifyPassword("some-password", storedHash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_Returns_False_For_NonNumeric_Iterations()
    {
        string storedHash = "abc.YWJjZA==.YWJjZA==";

        bool result = Utilities.VerifyPassword("some-password", storedHash);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void VerifyPassword_Returns_False_For_Zero_Or_Negative_Iterations(string iterations)
    {
        string storedHash = $"{iterations}.YWJjZA==.YWJjZA==";

        bool result = Utilities.VerifyPassword("some-password", storedHash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_Returns_False_For_Invalid_Base64()
    {
        string storedHash = "100000.YWJjZA==.@@@@=";

        bool result = Utilities.VerifyPassword("some-password", storedHash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_Returns_False_For_Wrong_Hash_Length()
    {
        string tooShortHash = Convert.ToBase64String(new byte[31]);

        string storedHash = $"100000.YWJjZA==.{tooShortHash}";

        bool result = Utilities.VerifyPassword("some-password", storedHash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_Returns_False_For_Tampered_Hash()
    {
        const string password = "tamper-me";
        string hash = Utilities.HashPassword(password);
        string[] parts = hash.Split('.');
        byte[] hashBytes = Convert.FromBase64String(parts[2]);
        hashBytes[0] ^= 0xFF;
        string tampered = $"{parts[0]}.{parts[1]}.{Convert.ToBase64String(hashBytes)}";

        bool result = Utilities.VerifyPassword(password, tampered);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_Rejects_Wrong_Password()
    {
        string storedHash = Utilities.HashPassword("correct-password");

        bool result = Utilities.VerifyPassword("wrong-password", storedHash);

        result.Should().BeFalse();
    }
}
