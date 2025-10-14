using IdentityService.Application.Interfaces;

namespace IdentityService.Infrastructure.Services
{
    public class PasswordHasher: IPasswordHasher
    {
        // Work factor for BCrypt - higher is more secure but slower
        // 12 is a good balance between security and performance
        private const int WorkFactor = 12;

        public string HashPassword(string password)
        {
            if(string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty",nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        }

        public bool VerifyPassword(string password,string hash)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (string.IsNullOrEmpty(hash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                // If the hash is malformed, return false rather than throwing
                return false;
            }
        }
    }
}