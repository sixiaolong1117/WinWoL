using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Security.Credentials.UI;
using Windows.Storage;

namespace WinWoL.Methods
{
    public static class WindowsHelloHelper
    {
        private const string WindowsHelloEnabledKey = "WindowsHelloEnabled";

        public static async Task<bool> IsAvailableAsync()
        {
            UserConsentVerifierAvailability availability = await UserConsentVerifier.CheckAvailabilityAsync();
            return availability == UserConsentVerifierAvailability.Available;
        }

        public static async Task<bool> VerifyAsync(string message)
        {
            if (!IsEnabled)
            {
                return true;
            }

            UserConsentVerificationResult consentResult = await UserConsentVerifier.RequestVerificationAsync(message);
            return consentResult == UserConsentVerificationResult.Verified;
        }

        public static async Task<bool> EnableAsync(string message)
        {
            UserConsentVerificationResult consentResult = await UserConsentVerifier.RequestVerificationAsync(message);
            if (consentResult == UserConsentVerificationResult.Verified)
            {
                IsEnabled = true;
                return true;
            }
            return false;
        }

        public static async Task<bool> DisableAsync(string message)
        {
            UserConsentVerificationResult consentResult = await UserConsentVerifier.RequestVerificationAsync(message);
            if (consentResult == UserConsentVerificationResult.Verified)
            {
                IsEnabled = false;
                return true;
            }
            return false;
        }

        public static bool IsEnabled
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                return localSettings.Values[WindowsHelloEnabledKey] as bool? ?? false;
            }
            private set
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values[WindowsHelloEnabledKey] = value;
            }
        }
    }
}
