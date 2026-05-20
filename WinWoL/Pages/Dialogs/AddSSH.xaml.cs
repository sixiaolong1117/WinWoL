using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Resources;
using WinWoL.Datas;
using WinWoL.Methods;
using WinWoL.Models;

namespace WinWoL.Pages.Dialogs
{
    public sealed partial class AddSSH : ContentDialog
    {
        public SSHModel SSHData { get; private set; }
        private readonly ResourceLoader resourceLoader = new ResourceLoader();
        private List<SSHKeyModel> sshKeys = new List<SSHKeyModel>();

        public AddSSH(SSHModel sshModel)
        {
            this.InitializeComponent();
            PrimaryButtonClick += MyDialog_PrimaryButtonClick;
            SecondaryButtonClick += MyDialog_SecondaryButtonClick;

            // 初始化Dialog中的字段，使用传入的SSHModel对象的属性
            SSHData = sshModel;
            ConfigNameTextBox.Text = sshModel.Name;
            IpAddressTextBox.Text = sshModel.IPAddress;
            SSHCommandTextBox.Text = sshModel.SSHCommand;
            SSHPortTextBox.Text = sshModel.SSHPort;
            SSHUserTextBox.Text = sshModel.SSHUser;
            PrivateKeyIsOpenToggleSwitch.IsOn = sshModel.SSHKeyIsOpen == "True";
            LoadSSHKeys(GetConfiguredSSHKeyId(sshModel));

            refresh();
        }

        private void MyDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 在"确定"按钮点击事件中保存用户输入的内容
            SSHData.Name = string.IsNullOrEmpty(ConfigNameTextBox.Text) ? "<未命名配置>" : ConfigNameTextBox.Text;
            SSHData.IPAddress = IpAddressTextBox.Text;
            SSHData.SSHCommand = SSHCommandTextBox.Text;
            SSHData.SSHPort = SSHPortTextBox.Text;
            SSHData.SSHUser = SSHUserTextBox.Text;
            SSHData.SSHKeyId = GetSelectedSSHKeyId();
            SSHData.SSHKeyPath = "";
            SSHData.SSHKeyIsOpen = PrivateKeyIsOpenToggleSwitch.IsOn ? "True" : "False";
        }

        private void MyDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 在"取消"按钮点击事件中不做任何操作
        }

        private void refresh()
        {
            // 是否启用功能
            PrivateKeyIsOpen();
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

        private string GetConfiguredSSHKeyId(SSHModel sshModel)
        {
            if (!string.IsNullOrEmpty(sshModel.SSHKeyId))
            {
                return sshModel.SSHKeyId;
            }

            return int.TryParse(sshModel.SSHKeyPath, out _) ? sshModel.SSHKeyPath : "";
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
    }
}
