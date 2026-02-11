using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Handles encryption and decryption of template URLs using password-based keys
    /// </summary>
    public static class TemplateEncryption
    {
        /// <summary>
        /// Hashes a password using SHA256
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Verifies a password against a stored hash
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            var computedHash = HashPassword(password);
            return computedHash == storedHash;
        }

        /// <summary>
        /// Creates a SHA256 symmetric key from a password by hashing each character and XORing them together
        /// </summary>
        public static byte[] CreateEncryptionKey(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty");

            using (var sha256 = SHA256.Create())
            {
                // Start with zeros
                byte[] key = new byte[32]; // SHA256 produces 32 bytes

                // Hash each character and XOR with the key
                foreach (char c in password)
                {
                    var charBytes = Encoding.UTF8.GetBytes(new[] { c });
                    var charHash = sha256.ComputeHash(charBytes);
                    
                    for (int i = 0; i < key.Length; i++)
                    {
                        key[i] ^= charHash[i];
                    }
                }

                return key;
            }
        }

        /// <summary>
        /// Encrypts a URL using AES with the password-derived key
        /// </summary>
        public static string EncryptUrl(string url, string password)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            try
            {
                var key = CreateEncryptionKey(password);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV();
                    
                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    {
                        var plainBytes = Encoding.UTF8.GetBytes(url);
                        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                        
                        // Combine IV and encrypted data
                        var result = new byte[aes.IV.Length + encryptedBytes.Length];
                        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
                        
                        return Convert.ToBase64String(result);
                    }
                }
            }
            catch
            {
                // If encryption fails, return original URL
                return url;
            }
        }

        /// <summary>
        /// Decrypts a URL using AES with the password-derived key
        /// </summary>
        public static string DecryptUrl(string encryptedUrl, string password)
        {
            if (string.IsNullOrEmpty(encryptedUrl))
                return encryptedUrl;

            try
            {
                var key = CreateEncryptionKey(password);
                var fullData = Convert.FromBase64String(encryptedUrl);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    
                    // Extract IV (first 16 bytes for AES)
                    var iv = new byte[aes.IV.Length];
                    Buffer.BlockCopy(fullData, 0, iv, 0, iv.Length);
                    aes.IV = iv;
                    
                    // Extract encrypted data
                    var encryptedBytes = new byte[fullData.Length - iv.Length];
                    Buffer.BlockCopy(fullData, iv.Length, encryptedBytes, 0, encryptedBytes.Length);
                    
                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch
            {
                // If decryption fails, return original string
                return encryptedUrl;
            }
        }
    }
}