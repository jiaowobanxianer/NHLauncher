using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Launcher.Shared
{
    public class PasswordHelper
    {
        public static (string hash, string salt) HashPassword(string password)
        {
            // 随机生成 salt
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            // 用 PBKDF2 生成 hash
            using var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
            string hash = Convert.ToBase64String(deriveBytes.GetBytes(32));

            return (hash, salt);
        }

        public static bool VerifyPassword(string password, string hash, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            using var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
            string newHash = Convert.ToBase64String(deriveBytes.GetBytes(32));
            return newHash == hash;
        }
        public static string HashPasswordClient(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

    }

}
