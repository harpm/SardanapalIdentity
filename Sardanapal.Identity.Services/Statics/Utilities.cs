﻿using System.Security.Cryptography;
using System.Text;

namespace Sardanapal.Identity.Services.Statics;
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
}