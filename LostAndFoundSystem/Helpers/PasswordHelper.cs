using System.Security.Cryptography;

namespace LostAndFoundSystem.Helpers
{
    /// <summary>
    /// Secure password hashing (PBKDF2-SHA256). No extra NuGet package needed.
    /// </summary>
    public static class PasswordHelper
    {
        private const string Prefix = "PBKDF2";
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100000;

        public static string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return string.Join("$",
                Prefix,
                Iterations.ToString(),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key));
        }

        public static bool IsHashed(string? stored)
        {
            return !string.IsNullOrEmpty(stored) && stored.StartsWith(Prefix + "$");
        }

        public static bool Verify(string password, string? stored)
        {
            if (string.IsNullOrEmpty(stored))
            {
                return false;
            }

            // Old accounts (e.g. the admin you inserted manually) still have a
            // plain-text password. They can still log in, and the password is
            // upgraded to a hash automatically on a successful login.
            if (!IsHashed(stored))
            {
                return stored == password;
            }

            try
            {
                string[] parts = stored.Split('$');

                if (parts.Length != 4 || !int.TryParse(parts[1], out int iterations))
                {
                    return false;
                }

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);

                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expected.Length);

                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
