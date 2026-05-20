using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinWoL.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Resources;
using WinWoL.Datas;
using WinWoL.Methods;

namespace WinWoL.Pages.Dialogs
{
    public sealed partial class AddWoL : ContentDialog
    {
        public WoLModel WoLData { get; private set; }
        private readonly ResourceLoader resourceLoader = new ResourceLoader();
        private List<SSHKeyModel> sshKeys = new List<SSHKeyModel>();

        public AddWoL(WoLModel wolModel)
        {
            this.InitializeComponent();
            PrimaryButtonClick += MyDialog_PrimaryButtonClick;
            SecondaryButtonClick += MyDialog_SecondaryButtonClick;

            // 初始化Dialog中的字段，使用传入的WoLModel对象的属性
            WoLData = wolModel;
            ConfigNameTextBox.Text = wolModel.Name;
            IpAddressTextBox.Text = wolModel.IPAddress;
            IndependentAddressCheckBox.IsChecked = wolModel.BroadcastIsOpen == "True";
            WoLIsOpenToggleSwitch.IsOn = wolModel.WoLIsOpen == "True";
            MacAddressTextBox.Text = wolModel.MacAddress;
            WoLPortTextBox.Text = wolModel.WoLPort;
            RDPIsOpenToggleSwitch.IsOn = wolModel.RDPIsOpen == "True";
            RDPIPPortTextBox.Text = wolModel.RDPPort;
            SSHShutdownIsOpenToggleSwitch.IsOn = wolModel.SSHIsOpen == "True";
            SSHCommandTextBox.Text = wolModel.SSHCommand;
            SSHPortTextBox.Text = wolModel.SSHPort;
            SSHUserTextBox.Text = wolModel.SSHUser;
            PrivateKeyIsOpenToggleSwitch.IsOn = wolModel.SSHKeyIsOpen == "True";
            LoadSSHKeys(GetConfiguredSSHKeyId(wolModel));

            // 根据是否开启独立的WoL地址
            if (IndependentAddressCheckBox.IsChecked == true)
            {
                // 开启，写入独立的WoL地址
                IndependentAddressTextBox.Text = wolModel.WoLAddress;
            }

            refresh();
        }
        private void MyDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 在"确定"按钮点击事件中保存用户输入的内容
            WoLData.Name = string.IsNullOrEmpty(ConfigNameTextBox.Text) ? "<未命名配置>" : ConfigNameTextBox.Text;
            WoLData.IPAddress = IpAddressTextBox.Text;
            WoLData.BroadcastIsOpen = IndependentAddressCheckBox.IsChecked == true ? "True" : "False";
            WoLData.WoLIsOpen = WoLIsOpenToggleSwitch.IsOn ? "True" : "False";
            WoLData.MacAddress = MacAddressTextBox.Text;
            WoLData.WoLPort = WoLPortTextBox.Text;
            WoLData.RDPIsOpen = RDPIsOpenToggleSwitch.IsOn ? "True" : "False";
            WoLData.RDPPort = RDPIPPortTextBox.Text;
            WoLData.SSHIsOpen = SSHShutdownIsOpenToggleSwitch.IsOn ? "True" : "False";
            WoLData.SSHCommand = SSHCommandTextBox.Text;
            WoLData.SSHPort = SSHPortTextBox.Text;
            WoLData.SSHUser = SSHUserTextBox.Text;
            WoLData.SSHKeyId = GetSelectedSSHKeyId();
            WoLData.SSHKeyPath = "";
            WoLData.SSHKeyIsOpen = PrivateKeyIsOpenToggleSwitch.IsOn ? "True" : "False";

            // 根据是否开启独立的WoL地址
            if (IndependentAddressCheckBox.IsChecked == true)
            {
                // 开启，写入独立的WoL地址
                WoLData.WoLAddress = IndependentAddressTextBox.Text;
            }
            else
            {
                // 关闭，写入IP地址
                WoLData.WoLAddress = IpAddressTextBox.Text;
            }
        }

        private void MyDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 在"取消"按钮点击事件中不做任何操作
        }
        private void refresh()
        {
            // 是否启用功能
            WoLIsOpen();
            RDPIsOpen();
            ShutdownIsOpen();
            PrivateKeyIsOpen();
            IndependentAddressIsChecked();
        }
        private void IndependentAddressIsChecked()
        {
            if (IndependentAddressCheckBox.IsChecked == true)
            {
                IndependentAddressTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                IndependentAddressTextBox.Visibility = Visibility.Collapsed;
            }
        }
        private void WoLIsOpen()
        {
            if (WoLIsOpenToggleSwitch.IsOn == true)
            {
                WoLConfig.Visibility = Visibility.Visible;
            }
            else
            {
                WoLConfig.Visibility = Visibility.Collapsed;
            }
        }
        private void RDPIsOpen()
        {
            if (RDPIsOpenToggleSwitch.IsOn == true)
            {
                RDPConfig.Visibility = Visibility.Visible;
            }
            else
            {
                RDPConfig.Visibility = Visibility.Collapsed;
            }
        }
        private void ShutdownIsOpen()
        {
            if (SSHShutdownIsOpenToggleSwitch.IsOn == true)
            {
                shutdownConfig.Visibility = Visibility.Visible;
            }
            else
            {
                shutdownConfig.Visibility = Visibility.Collapsed;
            }
        }
        private void PrivateKeyIsOpen()
        {
            if (PrivateKeyIsOpenToggleSwitch.IsOn == true)
            {
                SSHKey.Visibility = Visibility.Visible;
                SSHPasswordBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                SSHKey.Visibility = Visibility.Collapsed;
                SSHPasswordBox.Visibility = Visibility.Visible;
            }
        }
        private void TextChanged(object sender, TextChangedEventArgs e)
        {
        }
        private void IndependentAddressTextChanged(object sender, TextChangedEventArgs e)
        {
        }
        private void MacAddressTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            MacAddressTextClean(textBox);
        }
        private void IndependentAddressTextPaste(object sender, TextControlPasteEventArgs e)
        {
        }
        private void MacAddressTextPaste(object sender, TextControlPasteEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            MacAddressTextClean(textBox);
        }
        private void MacAddressTextClean(TextBox textBox)
        {
            string input = textBox.Text;

            // 使用正则表达式来匹配合法的格式
            string pattern = @"^([0-9A-Fa-f]{2}[:]){5}([0-9A-Fa-f]{2})$";
            if (Regex.IsMatch(input, pattern))
            {
                // 输入合法，保持文本不变
                textBox.Text = input;
            }
            else
            {
                // 输入非法，移除不匹配的字符
                textBox.Text = Regex.Replace(input, "[^0-9A-Fa-f:]", "");
                // 光标移动至末尾
                textBox.SelectionStart = textBox.Text.Length;
            }
        }
        private void IPAddressTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            IPAddressTextClean(textBox);
        }
        private void IPAddressTextPaste(object sender, TextControlPasteEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            IPAddressTextClean(textBox);
        }
        private void IPAddressTextClean(TextBox textBox)
        {
            string input = textBox.Text;

            // 使用正则表达式来匹配合法的格式
            string pattern = @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$|^((?!-)[A-Za-z0-9-]{1,63}(?<!-)\.)+[A-Za-z]{2,6}$";
            if (Regex.IsMatch(input, pattern))
            {
                // 输入合法，保持文本不变
                textBox.Text = input;
            }
            else
            {
                // 输入非法，移除不匹配的字符
                textBox.Text = Regex.Replace(input, @"[^A-Za-z0-9:.]", "");
                // 光标移动至末尾
                textBox.SelectionStart = textBox.Text.Length;
            }
        }
        private void PortTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            PortTextClean(textBox);
        }
        private void PortTextPaste(object sender, TextControlPasteEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            PortTextClean(textBox);
        }
        private void PortTextClean(TextBox textBox)
        {
            string input = textBox.Text;

            // 使用正则表达式来匹配合法的格式
            string pattern = "^[0-9]*$";
            if (Regex.IsMatch(input, pattern))
            {
                // 输入合法，保持文本不变
                textBox.Text = input;
            }
            else
            {
                // 输入非法，移除不匹配的字符
                textBox.Text = Regex.Replace(input, "[^0-9]", "");
                // 光标移动至末尾
                textBox.SelectionStart = textBox.Text.Length;
            }
        }
        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            refresh();
        }
        // 启用独立的WoL地址
        private void IndependentAddress_Checked(object sender, RoutedEventArgs e)
        {
            // WoL地址单独配置
            IndependentAddressTextBox.Visibility = Visibility.Visible;
            refresh();
        }
        // 关闭独立的WoL地址
        private void IndependentAddress_Unchecked(object sender, RoutedEventArgs e)
        {
            // WoL地址与IP地址相同
            IndependentAddressTextBox.Visibility = Visibility.Collapsed;
            refresh();
        }
        private void rdpIsOpen_Toggled(object sender, RoutedEventArgs e)
        {
            refresh();
        }
        private void wolIsOpen_Toggled(object sender, RoutedEventArgs e)
        {
            refresh();
        }
        private void SSHShutdownIsOpen_Toggled(object sender, RoutedEventArgs e)
        {
            refresh();
        }
        private void privateKeyIsOpen_Toggled(object sender, RoutedEventArgs e)
        {
            refresh();
        }
        private async void ImportSSHKey_Click(object sender, RoutedEventArgs e)
        {
            int? sshKeyId = await SSHKeyMethod.ImportKey();
            if (sshKeyId != null)
            {
                LoadSSHKeys(sshKeyId.Value.ToString());
            }
        }

        private void ConfirmPasteSSHKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int sshKeyId = SSHKeyMethod.SavePrivateKey(SSHKeyNameTextBox.Text, SSHPrivateKeyTextBox.Text);
                SSHKeyNameTextBox.Text = "";
                SSHPrivateKeyTextBox.Text = "";
                PasteSSHKeyError.Visibility = Visibility.Collapsed;
                PasteSSHKeyFlyout.Hide();
                LoadSSHKeys(sshKeyId.ToString());
            }
            catch (Exception ex)
            {
                PasteSSHKeyError.Text = string.Format(resourceLoader.GetString("PasteSSHKeyError"), ex.Message);
                PasteSSHKeyError.Visibility = Visibility.Visible;
            }
        }

        private void DeleteSSHKey_Click(object sender, RoutedEventArgs e)
        {
            if (SSHKeyComboBox.SelectedItem is SSHKeyModel selectedKey)
            {
                SQLiteHelper dbHelper = new SQLiteHelper();
                dbHelper.DeleteSSHKey(selectedKey.Id);
                LoadSSHKeys("");
            }
        }

        private void SSHKeyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteSSHKeyButton.IsEnabled = SSHKeyComboBox.SelectedItem != null;
        }

        private void LoadSSHKeys(string selectedSSHKeyId)
        {
            SQLiteHelper dbHelper = new SQLiteHelper();
            sshKeys = dbHelper.QuerySSHKeys();
            SSHKeyComboBox.ItemsSource = sshKeys;
            SSHKeyComboBox.SelectedItem = null;

            foreach (SSHKeyModel sshKey in sshKeys)
            {
                if (sshKey.Id.ToString() == selectedSSHKeyId)
                {
                    SSHKeyComboBox.SelectedItem = sshKey;
                    break;
                }
            }

            DeleteSSHKeyButton.IsEnabled = SSHKeyComboBox.SelectedItem != null;
        }

        private string GetSelectedSSHKeyId()
        {
            if (SSHKeyComboBox.SelectedItem is SSHKeyModel selectedKey)
            {
                return selectedKey.Id.ToString();
            }
            return "";
        }

        private string GetConfiguredSSHKeyId(WoLModel wolModel)
        {
            if (!string.IsNullOrEmpty(wolModel.SSHKeyId))
            {
                return wolModel.SSHKeyId;
            }

            return int.TryParse(wolModel.SSHKeyPath, out _) ? wolModel.SSHKeyPath : "";
        }
    }
}
