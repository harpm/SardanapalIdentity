using System.Reflection;
using System.Security.Cryptography;
using FluentAssertions;
using Sardanapal.Identity.Share.Static;
using Xunit;

namespace Sardanapal.Identity.Share.Tests.Unit;

public class PasswordSecurityTests
{
    [Fact]
    public void Password_Uses_PBKDF2_HMAC_SHA256_With_100k_Iterations_16b_Salt_32b_Hash()
    {
        const string password = "security-check";

        string hash = Utilities.HashPassword(password);

        string[] parts = hash.Split('.');
        parts.Should().HaveCount(3);
        parts[0].Should().Be("100000");

        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] hashBytes = Convert.FromBase64String(parts[2]);
        salt.Should().HaveCount(16);
        hashBytes.Should().HaveCount(32);

        byte[] recomputed = Rfc2898DeriveBytes.Pbkdf2(
            System.Text.Encoding.UTF8.GetBytes(password), salt, 100_000, HashAlgorithmName.SHA256, 32);
        recomputed.Should().Equal(hashBytes);
    }

    [Fact]
    public void Password_Compare_Is_Constant_Time()
    {
        MethodInfo? method = typeof(Utilities).GetMethod(nameof(Utilities.VerifyPassword), BindingFlags.Public | BindingFlags.Static);

        method.Should().NotBeNull();
        MethodInfo? fixedTimeEquals = typeof(CryptographicOperations)
            .GetMethod(nameof(CryptographicOperations.FixedTimeEquals), BindingFlags.Public | BindingFlags.Static);

        fixedTimeEquals.Should().NotBeNull();

        const string password = "compare-me";
        string hash = Utilities.HashPassword(password);

        bool valid = Utilities.VerifyPassword(password, hash);
        bool invalid = Utilities.VerifyPassword(password + "x", hash);

        valid.Should().BeTrue();
        invalid.Should().BeFalse();
    }

    [Fact]
    public async Task EncryptToMd5_Remarks_Cryptographically_Broken_And_Obsolete()
    {
        MethodInfo? method = typeof(Utilities).GetMethod(nameof(Utilities.EncryptToMd5));

        method.Should().BeDecoratedWith<ObsoleteAttribute>();
        ObsoleteAttribute attr = method!.GetCustomAttribute<ObsoleteAttribute>()!;
        attr.Message.Should().Contain("broken", "the obsolete message must flag MD5 as cryptographically broken");

        await Task.CompletedTask;
    }
}
