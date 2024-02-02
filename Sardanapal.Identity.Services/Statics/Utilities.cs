using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Sardanapal.Identity.Services.Statics
{
    public static class Utilities
    {
        public static async Task<string> EncryptToMd5(string input)
        {
            using MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }

            return sb.ToString();
        }

        public static async Task<string> GenerateToken(string key, string issuer, string audience, string username,
            string role, int expireMinutes)
        {
            var SymmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var Credentials = new SigningCredentials(SymmetricKey, SecurityAlgorithms.HmacSha256);
            var Claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
            };
            var token = new JwtSecurityToken(issuer, audience, Claims, expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: Credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
