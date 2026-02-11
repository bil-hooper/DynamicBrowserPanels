using System;
using System.Security.Cryptography;
using System.Text;

namespace DynamicBrowserPanels
{
    public class PrivacyLockManager
    {
        private static PrivacyLockManager _instance;
        private string _pinHash; // Decrypted hash kept in memory
        
        public event EventHandler LockRequested;
        public event EventHandler UnlockSuccessful;
        
        public bool IsLocked { get; private set; }
        public bool IsEnabled { get; private set; }
        
        public static PrivacyLockManager Instance => _instance ??= new PrivacyLockManager();
        
        private PrivacyLockManager() 
        {
            LoadFromConfiguration();
        }
        
        private void LoadFromConfiguration()
        {
            try
            {
                var settings = AppConfiguration.PrivacyLockSettings;
                
                IsEnabled = settings.Enabled;
                
                if (IsEnabled && !string.IsNullOrEmpty(settings.PinHashEncrypted))
                {
                    _pinHash = Unprotect(settings.PinHashEncrypted);
                }
            }
            catch
            {
                // If decryption fails, reset
                _pinHash = null;
                IsEnabled = false;
            }
        }
        
        private void SaveToConfiguration()
        {
            var settings = new PrivacyLockSettings
            {
                Enabled = IsEnabled,
                PinHashEncrypted = IsEnabled && !string.IsNullOrEmpty(_pinHash) 
                    ? Protect(_pinHash) 
                    : string.Empty
            };
            
            AppConfiguration.PrivacyLockSettings = settings;
        }
        
        public void Initialize(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
                throw new ArgumentException("PIN cannot be empty");
            
            if (pin.Length < 4)
                throw new ArgumentException("PIN must be at least 4 characters");
                
            _pinHash = HashPin(pin);
            IsEnabled = true;
            SaveToConfiguration();
        }
        
        public void Disable()
        {
            IsEnabled = false;
            _pinHash = null;
            SaveToConfiguration();
        }
        
        public void Lock()
        {
            if (!IsEnabled)
                throw new InvalidOperationException("Privacy lock is not configured. Set a PIN first.");
            
            if (IsLocked)
                return;
            
            IsLocked = true;
            LockRequested?.Invoke(this, EventArgs.Empty);
        }
        
        public bool Unlock(string pin)
        {
            if (!IsLocked || !IsEnabled)
                return true;
            
            if (VerifyPin(pin))
            {
                IsLocked = false;
                UnlockSuccessful?.Invoke(this, EventArgs.Empty);
                return true;
            }
            
            return false;
        }
        
        public bool ChangePin(string oldPin, string newPin)
        {
            if (!IsEnabled || !VerifyPin(oldPin))
                return false;
            
            if (string.IsNullOrWhiteSpace(newPin) || newPin.Length < 4)
                return false;
            
            _pinHash = HashPin(newPin);
            SaveToConfiguration();
            return true;
        }
        
        private string HashPin(string pin)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(pin);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        /// <summary>
        /// Verifies if the provided PIN matches the stored PIN hash
        /// </summary>
        public bool VerifyPin(string pin)
        {
            if (string.IsNullOrEmpty(_pinHash))
                return false;
                
            return HashPin(pin) == _pinHash;
        }
        
        /// <summary>
        /// Use DPAPI to encrypt the hash (user-specific, machine-specific)
        /// </summary>
        private string Protect(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        
        /// <summary>
        /// Use DPAPI to decrypt the hash
        /// </summary>
        private string Unprotect(string encryptedData)
        {
            var bytes = Convert.FromBase64String(encryptedData);
            var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}