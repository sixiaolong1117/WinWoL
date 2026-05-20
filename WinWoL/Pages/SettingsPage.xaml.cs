using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.Globalization;
using Windows.Storage;
using WinWoL.Datas;
using WinWoL.Pages.Dialogs;

namespace WinWoL.Pages
{
    public sealed partial class SettingsPage : Page
    {
        // 启用本地设置数据
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        ResourceLoader resourceLoader = new ResourceLoader();

        public SettingsPage()
        {
            this.InitializeComponent();

            LoadString();
            materialStatusSet();
            languageStatusSet();
        }
        // 材料ComboBox列表List
        public List<string> material { get; } = new List<string>()
        {
            "Mica",
            "Mica Alt",
            "Acrylic"
        };

        public List<string> language { get; } = new List<string>()
        {
            "简体中文",
            "English",
            "日本語",
            "한국어"
        };
        private void LoadString()
        {
            ResetDatabaseTips.ActionButtonContent = resourceLoader.GetString("Confirm");
            ResetDatabaseTips.CloseButtonContent = resourceLoader.GetString("Cancel");
        }
        private void languageStatusSet()
        {
            if (!languageStatusSetList())
            {
                // 未设置
                localSettings.Values["languageChange"] = Windows.Globalization.Language.CurrentInputMethodLanguageTag;
                // 非法输入，扔出警报
                //throw new Exception(Windows.Globalization.Language.CurrentInputMethodLanguageTag);
                languageStatusSetList();
            }
        }
        private bool languageStatusSetList()
        {
            // 读取本地设置数据，调整ComboBox状态
            if (localSettings.Values["languageChange"] as string == "zh-Hans-CN")
            {
                languageChange.SelectedItem = language[0];
                return true;
            }
            else if (localSettings.Values["languageChange"] as string == "en-US")
            {
                languageChange.SelectedItem = language[1];
                return true;
            }
            else if (localSettings.Values["languageChange"] as string == "ja-JP")
            {
                languageChange.SelectedItem = language[2];
                return true;
            }
            else if (localSettings.Values["languageChange"] as string == "ko-KR")
            {
                languageChange.SelectedItem = language[3];
                return true;
            }
            else
            {
                return false;
            }
        }

        private void materialStatusSet()
        {
            // 读取本地设置数据，调整ComboBox状态
            if (localSettings.Values["materialStatus"] as string == "Mica")
            {
                backgroundMaterial.SelectedItem = material[0];
            }
            else if (localSettings.Values["materialStatus"] as string == "Mica Alt")
            {
                backgroundMaterial.SelectedItem = material[1];
            }
            else if (localSettings.Values["materialStatus"] as string == "Acrylic")
            {
                backgroundMaterial.SelectedItem = material[2];
            }
            else
            {
                // 非法输入，设置默认材料为Mica Alt
                localSettings.Values["materialStatus"] = "Mica Alt";
                backgroundMaterial.SelectedItem = material[1];
                // 非法输入，扔出警报
                //throw new Exception($"Wrong material type: {localSettings.Values["materialStatus"]}");
            }
        }

        // 背景材料设置ComboBox改动事件
        private void backgroundMaterial_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string materialStatus = e.AddedItems[0].ToString();
            switch (materialStatus)
            {
                case "Mica":
                    if (localSettings.Values["materialStatus"] as string != "Mica")
                    {
                        localSettings.Values["materialStatus"] = "Mica";
                        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
                    }
                    else
                    {
                        localSettings.Values["materialStatus"] = "Mica";
                    }
                    break;
                case "Mica Alt":
                    if (localSettings.Values["materialStatus"] as string != "Mica Alt")
                    {
                        localSettings.Values["materialStatus"] = "Mica Alt";
                        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
                    }
                    else
                    {
                        localSettings.Values["materialStatus"] = "Mica Alt";
                    }
                    break;
                case "Acrylic":
                    if (localSettings.Values["materialStatus"] as string != "Acrylic")
                    {
                        localSettings.Values["materialStatus"] = "Acrylic";
                        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
                    }
                    else
                    {
                        localSettings.Values["materialStatus"] = "Acrylic";
                    }
                    break;
                default:
                    throw new Exception($"Invalid argument: {materialStatus}");
            }
        }

        private void languageChange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string languageStatus = e.AddedItems[0].ToString();
            switch (languageStatus)
            {
                case "简体中文":
                    if (localSettings.Values["languageChange"] as string != "zh-Hans-CN")
                    {
                        localSettings.Values["languageChange"] = "zh-Hans-CN";
                        ApplicationLanguages.PrimaryLanguageOverride = localSettings.Values["languageChange"] as string;
                        Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", localSettings.Values["languageChange"] as string);
                        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
                    }
                    else
                    {
                        localSettings.Values["languageChange"] = "zh-Hans-CN";
                    }
                    break;
                case "English":
                    if (localSettings.Values["languageChange"] as string != "en-US")
                    {
                        localSettings.Values["languageChange"] = "en-US";
                        ApplicationLanguages.PrimaryLanguageOverride = localSettings.Values["languageChange"] as string;
                        Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", localSettings.Values["languageChange"] as string);
                        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
                    }
                    else
                    {
                        localSettings.Values["languageChange"] = "en-US";
                    }
                    break;
                case "日本語":
                    if (localSettings.Values["languageChange"] as string != "ja-JP")
                    {
                        localSettings.Values["languageChange"] = "ja-JP";
                        ApplicationLanguages.PrimaryLanguageOverride = localSettings.Values["languageChange"] as string;
                        Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", localSettings.Values["languageChange"] as string);
                        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
                    }
                    else
                    {
                        localSettings.Values["languageChange"] = "ja-JP";
                    }
                    break;
                case "한국어":
                    if (localSettings.Values["languageChange"] as string != "ko-KR")
                    {
                        localSettings.Values["languageChange"] = "ko-KR";
                        ApplicationLanguages.PrimaryLanguageOverride = localSettings.Values["languageChange"] as string;
                        Windows.ApplicationModel.Resources.Core.ResourceContext.SetGlobalQualifierValue("Language", localSettings.Values["languageChange"] as string);
                        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
                    }
                    else
                    {
                        localSettings.Values["languageChange"] = "ko-KR";
                    }
                    break;
                default:
                    throw new Exception($"Invalid argument: {languageChange}");
            }
        }
        private void ResetDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            ResetDatabaseTips.IsOpen = true;
        }

        private async void ManageSSHKeysButton_Click(object sender, RoutedEventArgs e)
        {
            ManageSSHKeys dialog = new ManageSSHKeys();
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.CloseButtonText = resourceLoader.GetString("Cancel");
            await dialog.ShowAsync();
        }

        private void ResetDatabaseTips_ActionButtonClick(TeachingTip sender, object args)
        {
            // 实例化SQLiteHelper
            SQLiteHelper dbHelper = new SQLiteHelper();
            dbHelper.DropTable();
            ResetDatabaseTips.IsOpen = false;
        }
    }
}
