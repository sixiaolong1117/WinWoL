using Renci.SshNet;
using Renci.SshNet.Security;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinWoL.Datas;
using WinWoL.Models;

namespace WinWoL.Methods
{
    public class SSHKeyMethod
    {
        public static async Task<int?> ImportKey()
        {
            var openPicker = new FileOpenPicker();
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.ViewMode = PickerViewMode.List;
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add("*");

            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file == null)
            {
                return null;
            }

            string privateKey = await FileIO.ReadTextAsync(file);
            try
            {
                return SavePrivateKey(file.Name, privateKey);
            }
            catch
            {
                return null;
            }
        }

        public static int SavePrivateKey(string name, string privateKey)
        {
            SSHKeyModel sshKey = CreateSSHKeyModel(name, privateKey);
            SQLiteHelper dbHelper = new SQLiteHelper();
            return dbHelper.InsertSSHKey(sshKey);
        }

        public static SSHKeyModel CreateSSHKeyModel(string name, string privateKey)
        {
            string normalizedPrivateKey = NormalizePrivateKey(privateKey);
            KeyMetadata keyMetadata = GetKeyMetadata(normalizedPrivateKey);
            string keyName = string.IsNullOrWhiteSpace(name) ? keyMetadata.Name : name.Trim();

            return new SSHKeyModel
            {
                Name = keyName,
                PrivateKey = normalizedPrivateKey,
                PublicKey = keyMetadata.PublicKey,
                Fingerprint = keyMetadata.Fingerprint,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        private static KeyMetadata GetKeyMetadata(string privateKey)
        {
            using (MemoryStream privateKeyStream = new MemoryStream(Encoding.UTF8.GetBytes(privateKey)))
            using (PrivateKeyFile privateKeyFile = new PrivateKeyFile(privateKeyStream))
            {
                HostAlgorithm hostAlgorithm = GetPublicKeyHostAlgorithm(privateKeyFile);
                string comment = privateKeyFile.Key.Comment;
                string publicKey = $"{hostAlgorithm.Name} {Convert.ToBase64String(hostAlgorithm.Data)}";
                if (!string.IsNullOrWhiteSpace(comment))
                {
                    publicKey += $" {comment}";
                }

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hash = sha256.ComputeHash(hostAlgorithm.Data);
                    string fingerprint = "SHA256:" + Convert.ToBase64String(hash).TrimEnd('=');
                    string name = string.IsNullOrWhiteSpace(comment) ? $"SSH Key {DateTime.Now:yyyyMMddHHmmss}" : comment;

                    return new KeyMetadata
                    {
                        Name = name,
                        PublicKey = publicKey,
                        Fingerprint = fingerprint
                    };
                }
            }
        }

        private static HostAlgorithm GetPublicKeyHostAlgorithm(PrivateKeyFile privateKeyFile)
        {
            HostAlgorithm sshRsaAlgorithm = privateKeyFile.HostKeyAlgorithms.FirstOrDefault(algorithm => algorithm.Name == "ssh-rsa");
            if (sshRsaAlgorithm != null)
            {
                return sshRsaAlgorithm;
            }

            HostAlgorithm hostAlgorithm = privateKeyFile.HostKeyAlgorithms.FirstOrDefault();
            if (hostAlgorithm == null)
            {
                throw new InvalidOperationException("无法识别 SSH 私钥。");
            }

            return hostAlgorithm;
        }

        private static string NormalizePrivateKey(string privateKey)
        {
            if (string.IsNullOrWhiteSpace(privateKey))
            {
                throw new InvalidOperationException("SSH 私钥内容为空。");
            }

            return privateKey.Replace("\r\n", "\n").Replace("\r", "\n").Trim() + "\n";
        }

        private class KeyMetadata
        {
            public string Name { get; set; }
            public string PublicKey { get; set; }
            public string Fingerprint { get; set; }
        }
    }
}
