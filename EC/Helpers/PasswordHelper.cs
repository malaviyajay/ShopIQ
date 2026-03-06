using System.Security.Cryptography;
using System.Text;

namespace EC.Helpers
{
    public class PasswordHelper
    {
        public static string Hash(string password)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }
}

