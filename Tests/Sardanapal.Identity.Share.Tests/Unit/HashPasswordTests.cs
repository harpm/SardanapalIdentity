using FluentAssertions;
using Sardanapal.Identity.Share.Static;
using Xunit;

namespace Sardanapal.Identity.Share.Tests.Unit;

public class HashPasswordTests
{
    [Fact]
    public void HashPassword_RoundTrips_Through_VerifyPassword()
    {
        const string password = "S3cret-Pa$$";

        string hash = Utilities.HashPassword(password);

        Utilities.VerifyPassword(password, hash).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HashPassword_Throws_ArgumentNullException_For_Null_Or_Empty(string? password)
    {
        Action act = () => Utilities.HashPassword(password!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(password));
    }

    [Fact]
    public void HashPassword_Output_Has_Three_Dot_Separated_Parts()
    {
        string hash = Utilities.HashPassword("anything");

        string[] parts = hash.Split('.');

        parts.Should().HaveCount(3);
    }

    [Fact]
    public void HashPassword_Stored_Iterations_Equals_100000()
    {
        string hash = Utilities.HashPassword("anything");

        string iterations = hash.Split('.')[0];

        iterations.Should().Be("100000");
    }

    [Fact]
    public void HashPassword_Produces_Different_Hashes_For_Same_Password()
    {
        const string password = "same-password";

        string first = Utilities.HashPassword(password);
        string second = Utilities.HashPassword(password);

        first.Should().NotBe(second);
    }

    [Fact]
    public void HashPassword_Salt_Is_16_Bytes_Hash_Is_32_Bytes()
    {
        string hash = Utilities.HashPassword("anything");

        string[] parts = hash.Split('.');
        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] hashBytes = Convert.FromBase64String(parts[2]);

        salt.Should().HaveCount(16);
        hashBytes.Should().HaveCount(32);
    }
}
