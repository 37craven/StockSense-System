using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;
using StockSense.Infrastructure.Data;

namespace StockSense.Web.Utility.Security
{
    // Handles passwords by pre-hashing with SHA-256, then securing with BCrypt
    public class BCryptPasswordHasher : IPasswordHasher<ApplicationUser>
    {
        public string HashPassword(ApplicationUser user, string password)
        {
            // 1. Convert the plain text password into a fixed-length SHA-256 string
            string sha256Hash = ComputeSha256(password);

            // 2. Feed that SHA-256 string into BCrypt with your work factor
            int workFactor = 12;
            return BCrypt.Net.BCrypt.HashPassword(sha256Hash, workFactor);
        }

        public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
        {
            try
            {
                // 1. Pre-hash the password the user just typed into the login screen
                string sha256HashOfProvided = ComputeSha256(providedPassword);

                // 2. Verify that SHA-256 hash against the BCrypt hash in the database
                bool isValid = BCrypt.Net.BCrypt.Verify(sha256HashOfProvided, hashedPassword);

                if (isValid)
                {
                    return PasswordVerificationResult.Success;
                }

                return PasswordVerificationResult.Failed;
            }
            catch
            {
                return PasswordVerificationResult.Failed;
            }
        }

        // Private helper method to handle the SHA-256 conversion
        private string ComputeSha256(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Using Base64 here is a bit cleaner and more compact than Hex for feeding into BCrypt
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
