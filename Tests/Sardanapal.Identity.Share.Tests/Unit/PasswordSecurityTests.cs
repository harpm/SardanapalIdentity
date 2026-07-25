using System.Reflection;
using System.Reflection.Emit;
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
    public void VerifyPassword_Invokes_CryptographicOperations_FixedTimeEquals()
    {
        MethodInfo? method = typeof(Utilities).GetMethod(
            nameof(Utilities.VerifyPassword), BindingFlags.Public | BindingFlags.Static);

        method.Should().NotBeNull();
        IReadOnlyList<MethodBase> calls = GetInvokedMethods(method!);

        bool usesFixedTimeEquals = calls.Any(c =>
            c.DeclaringType == typeof(CryptographicOperations)
            && c.Name == nameof(CryptographicOperations.FixedTimeEquals));

        usesFixedTimeEquals.Should().BeTrue(
            "VerifyPassword must compare hashes with CryptographicOperations.FixedTimeEquals for constant-time equality");

        const string password = "compare-me";
        string hash = Utilities.HashPassword(password);
        Utilities.VerifyPassword(password, hash).Should().BeTrue();
        Utilities.VerifyPassword(password + "x", hash).Should().BeFalse();
    }

    private static IReadOnlyList<MethodBase> GetInvokedMethods(MethodInfo method)
    {
        byte[]? il = method.GetMethodBody()?.GetILAsByteArray();
        if (il == null) return Array.Empty<MethodBase>();

        Module module = method.Module;
        List<MethodBase> calls = new List<MethodBase>();

        Dictionary<byte, OpCode> single = new Dictionary<byte, OpCode>();
        Dictionary<byte, OpCode> prefixed = new Dictionary<byte, OpCode>();
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode op)
            {
                int value = op.Value;
                if ((value & 0xFF00) != 0)
                    prefixed[(byte)(value & 0xFF)] = op;
                else
                    single[(byte)value] = op;
            }
        }

        int i = 0;
        while (i < il.Length)
        {
            byte b = il[i];
            i++;
            OpCode op;
            if (b == 0xFE)
            {
                if (i >= il.Length) break;
                byte b2 = il[i];
                i++;
                if (!prefixed.TryGetValue(b2, out op)) continue;
            }
            else if (!single.TryGetValue(b, out op))
            {
                continue;
            }

            switch (op.OperandType)
            {
                case OperandType.InlineMethod:
                    if (i + 4 <= il.Length)
                    {
                        int token = BitConverter.ToInt32(il, i);
                        ResolveCall(module, token, calls);
                    }
                    i += 4;
                    break;
                case OperandType.InlineNone:
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    i += 8;
                    break;
                case OperandType.InlineVar:
                    i += 2;
                    break;
                case OperandType.ShortInlineVar:
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                    i += 1;
                    break;
                case OperandType.InlineSwitch:
                    if (i + 4 <= il.Length)
                    {
                        int count = BitConverter.ToInt32(il, i);
                        i += 4 + count * 4;
                    }
                    else
                    {
                        i = il.Length;
                    }
                    break;
                default:
                    i += 4;
                    break;
            }
        }

        return calls;
    }

    private static void ResolveCall(Module module, int token, List<MethodBase> calls)
    {
        try
        {
            MethodBase? resolved = module.ResolveMethod(token);
            if (resolved != null) calls.Add(resolved);
        }
        catch (ArgumentException)
        {
        }
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
