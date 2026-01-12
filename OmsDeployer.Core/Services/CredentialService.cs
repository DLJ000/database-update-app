using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OmsDeployer.Core.Services
{
    public class CredentialService
    {
        private readonly string _configPath;

        public CredentialService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "OmsDeployer");
            Directory.CreateDirectory(appFolder);
            _configPath = Path.Combine(appFolder, "config.encrypted");
        }

        public void SaveCredentials(string ftpPassword, string rootPassword, string tomcatPassword)
        {
            var data = $"{ftpPassword}|{rootPassword}|{tomcatPassword}";
            var encrypted = Encrypt(data);
            File.WriteAllBytes(_configPath, encrypted);
        }

        public (string ftpPassword, string rootPassword, string tomcatPassword) LoadCredentials()
        {
            if (!File.Exists(_configPath))
                return (string.Empty, string.Empty, string.Empty);

            var encrypted = File.ReadAllBytes(_configPath);
            var decrypted = Decrypt(encrypted);
            var parts = decrypted.Split('|');
            
            return parts.Length == 3 
                ? (parts[0], parts[1], parts[2]) 
                : (string.Empty, string.Empty, string.Empty);
        }

        private byte[] Encrypt(string plainText)
        {
            byte[] encrypted;
            byte[] key = GetKey();
            byte[] iv = new byte[16];
            RandomNumberGenerator.Fill(iv);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    encrypted = ms.ToArray();
                }
            }
            return encrypted;
        }

        private string Decrypt(byte[] cipherText)
        {
            string plaintext;
            byte[] key = GetKey();

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                byte[] iv = new byte[16];
                Array.Copy(cipherText, 0, iv, 0, 16);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherText, 16, cipherText.Length - 16))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    plaintext = sr.ReadToEnd();
                }
            }
            return plaintext;
        }

        private byte[] GetKey()
        {
            // Use machine-specific key derived from machine name
            var machineName = Environment.MachineName;
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes($"OmsDeployer_{machineName}"));
            }
        }
    }
}

