using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using WinWoL.Datas;
using WinWoL.Methods;
using WinWoL.Models;

namespace WinWoL.Pages.Dialogs
{
    public sealed partial class ManageSSHKeys : ContentDialog
    {
        private readonly ResourceLoader resourceLoader = new ResourceLoader();
        private List<SSHKeyModel> sshKeys = new List<SSHKeyModel>();

        public ManageSSHKeys()
        {
            this.InitializeComponent();
            LoadSSHKeys();
        }

        private async void ImportSSHKey_Click(object sender, RoutedEventArgs e)
        {
            ImportSSHKeyButton.IsEnabled = false;
            try
            {
                int? sshKeyId = await SSHKeyMethod.ImportKey();
                LoadSSHKeys(sshKeyId?.ToString());
            }
            finally
            {
                ImportSSHKeyButton.IsEnabled = true;
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
            if (SSHKeyListView.SelectedItem is SSHKeyModel selectedKey)
            {
                SQLiteHelper dbHelper = new SQLiteHelper();
                dbHelper.DeleteSSHKey(selectedKey.Id);
                LoadSSHKeys();
            }
        }

        private void SSHKeyListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteSSHKeyButton.IsEnabled = SSHKeyListView.SelectedItem != null;
        }

        private void LoadSSHKeys(string selectedSSHKeyId = null)
        {
            SQLiteHelper dbHelper = new SQLiteHelper();
            sshKeys = dbHelper.QuerySSHKeys();
            SSHKeyListView.ItemsSource = sshKeys;
            SSHKeyListView.SelectedItem = null;

            foreach (SSHKeyModel sshKey in sshKeys)
            {
                if (sshKey.Id.ToString() == selectedSSHKeyId)
                {
                    SSHKeyListView.SelectedItem = sshKey;
                    break;
                }
            }

            EmptySSHKeyTips.Visibility = sshKeys.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            SSHKeyListView.Visibility = sshKeys.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            DeleteSSHKeyButton.IsEnabled = SSHKeyListView.SelectedItem != null;
        }
    }
}
