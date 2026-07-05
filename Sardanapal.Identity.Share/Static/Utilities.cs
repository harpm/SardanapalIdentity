using System.Security.Cryptography;
using System.Text;

namespace Sardanapal.Identity.Share.Statics;
public static class Utilities
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, Iterations, Algorithm, HashSize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        var parts = storedHash.Split('.');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out int iterations)
            || iterations <= 0)
            return false;

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedHash = Convert.FromBase64String(parts[2]);
        }
        catch
        {
            return false;
        }

        if (expectedHash.Length != HashSize)
            return false;

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt, iterations, Algorithm, HashSize);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    [Obsolete("MD5 is cryptographically broken and unsalted. Use HashPassword/VerifyPassword instead.")]
    public static Task<string> EncryptToMd5(string input)
    {
        using MD5 md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            sb.Append(hashBytes[i].ToString("x2"));
        }

        return Task.FromResult(sb.ToString());
    }

    private const string PasswordCharsetLower = "abcdefghijklmnopqrstuvwxyz";
    private const string PasswordCharsetUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string PasswordCharsetDigits = "0123456789";

    public static string GenerateRandomPassword(int length = 16)
    {
        if (length < 4) length = 4;

        char[] all = (PasswordCharsetLower + PasswordCharsetUpper + PasswordCharsetDigits).ToCharArray();
        byte[] buffer = RandomNumberGenerator.GetBytes(length);

        char[] chars = new char[length];
        chars[0] = PasswordCharsetLower[buffer[0] % PasswordCharsetLower.Length];
        chars[1] = PasswordCharsetUpper[buffer[1] % PasswordCharsetUpper.Length];
        chars[2] = PasswordCharsetDigits[buffer[2] % PasswordCharsetDigits.Length];
        for (int i = 3; i < length; i++)
        {
            chars[i] = all[buffer[i] % all.Length];
        }

        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = buffer[i % buffer.Length] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
