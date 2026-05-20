using System;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage.Streams;

namespace WinWoL.Methods
{
    public static class SSHKeyProtection
    {
        private const string ProtectionDescriptor = "LOCAL=user";

        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return "";
            }

            DataProtectionProvider provider = new DataProtectionProvider(ProtectionDescriptor);
            IBuffer plainBuffer = CryptographicBuffer.ConvertStringToBinary(plainText, BinaryStringEncoding.Utf8);
            IBuffer protectedBuffer = provider.ProtectAsync(plainBuffer).AsTask().GetAwaiter().GetResult();
            CryptographicBuffer.CopyToByteArray(protectedBuffer, out byte[] protectedBytes);
            return Convert.ToBase64String(protectedBytes);
        }

        public static string Unprotect(string protectedText)
        {
            if (string.IsNullOrEmpty(protectedText))
            {
                return "";
            }

            byte[] protectedBytes = Convert.FromBase64String(protectedText);
            IBuffer protectedBuffer = CryptographicBuffer.CreateFromByteArray(protectedBytes);
            DataProtectionProvider provider = new DataProtectionProvider();
            IBuffer plainBuffer = provider.UnprotectAsync(protectedBuffer).AsTask().GetAwaiter().GetResult();
            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, plainBuffer);
        }
    }
}
