using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using WinWoL.Datas;
using WinWoL.Methods;
using WinWoL.Models;
using WinWoL.Pages.Dialogs;

namespace WinWoL.Pages
{
    public sealed partial class SSHShortcut : Page
    {
        // 启用本地设置数据
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        ResourceLoader resourceLoader = new ResourceLoader();
        private DispatcherQueue _dispatcherQueue;
        private SSHModel selectedSSHModel;
        public SSHShortcut()
        {
            this.InitializeComponent();

            // 获取UI线程的DispatcherQueue
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            LoadData();
            LoadString();
        }
        private void LoadData()
        {
            // 实例化SQLiteHelper
            SQLiteHelper dbHelper = new SQLiteHelper();
            // 查询数据
            List<SSHModel> dataList = dbHelper.QuerySSHData();

            // 将数据列表绑定到ListView
            dataGridView.ItemsSource = dataList;
        }
        private void LoadString()
        {
            Header.Text = resourceLoader.GetString("SSHHeader");
            SSHResponse.CloseButtonContent = resourceLoader.GetString("Confirm");
            SaveFileTips.CloseButtonContent = resourceLoader.GetString("Confirm");
        }
        // 添加/修改配置按钮点击
        private async void AddConfigButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建一个初始的WoLModel对象
            SSHModel initialWoLData = new SSHModel();

            // 创建一个新的dialog对象
            AddSSH dialog = new AddSSH(initialWoLData);
            // 对此dialog对象进行配置
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.PrimaryButtonText = resourceLoader.GetString("Add");
            dialog.CloseButtonText = resourceLoader.GetString("Cancel");
            // 默认按钮为PrimaryButton
            dialog.DefaultButton = ContentDialogButton.Primary;

            // 订阅 DataSaved 事件，处理从管理密钥页面返回后保存数据的情况
            dialog.DataSaved += (s, args) => LoadData();

            // 显示Dialog并等待其关闭
            ContentDialogResult result = await dialog.ShowAsync();

            // 如果按下了Primary
            if (result == ContentDialogResult.Primary)
            {
                // 实例化SQLiteHelper
                SQLiteHelper dbHelper = new SQLiteHelper();
                // 插入新数据
                dbHelper.InsertSSHData(initialWoLData);
                // 重新加载数据
                LoadData();
            }
        }
        private async void ConfirmReplace_Click(object sender, RoutedEventArgs e)
        {
            // 关闭二次确认Flyout
            confirmationReplaceFlyout.Hide();
            ImportConfig.IsEnabled = false;
            // 实例化SQLiteHelper
            SQLiteHelper dbHelper = new SQLiteHelper();

            // 获取导入的数据
            SSHModel newModel = await SSHMethod.ImportConfig();

            if (newModel != null)
            {
                // 获取当前配置的ID
                int id = selectedSSHModel.Id;
                // 赋给导入的配置
                newModel.Id = id;
                // 插入新数据
                dbHelper.UpdateSSHData(newModel);
                // 重新加载数据
                LoadData();
            }
            ImportConfig.IsEnabled = true;
        }

        private void ConfirmDelete_Click(object sender, RoutedEventArgs e)
        {
            // 关闭二次确认Flyout
            confirmationFlyout.Hide();
            // 实例化SQLiteHelper
            SQLiteHelper dbHelper = new SQLiteHelper();
            // 删除数据
            dbHelper.DeleteSSHData(selectedSSHModel);
            // 重新加载数据
            LoadData();
        }
        private void CancelReplace_Click(object sender, RoutedEventArgs e)
        {
            // 关闭二次确认Flyout
            confirmationReplaceFlyout.Hide();
        }
        private void CancelDelete_Click(object sender, RoutedEventArgs e)
        {
            // 关闭二次确认Flyout
            confirmationFlyout.Hide();
        }
        private async void SSHSend(SSHModel sshModel)
        {
            string sshPasswd = null;
            // 使用密码登录
            if (sshModel.SSHKeyIsOpen != "True")
            {
                SSHPasswdModel sshPasswdModel = new SSHPasswdModel();
                // 创建一个新的dialog对象
                EnterSSHPasswd dialog = new EnterSSHPasswd(sshPasswdModel);
                // 对此dialog对象进行配置
                dialog.XamlRoot = this.XamlRoot;
                dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                dialog.PrimaryButtonText = resourceLoader.GetString("Confirm");
                dialog.CloseButtonText = resourceLoader.GetString("Cancel");
                // 默认按钮为PrimaryButton
                dialog.DefaultButton = ContentDialogButton.Primary;

                // 显示Dialog并等待其关闭
                ContentDialogResult result = await dialog.ShowAsync();

                // 如果按下了Primary
                if (result == ContentDialogResult.Primary)
                {
                    sshPasswd = sshPasswdModel.SSHPasswd;
                    SSHSendThread(sshModel, sshPasswd);
                }
            }
            else
            {
                sshPasswd = null;
                SSHSendThread(sshModel, sshPasswd);
            }
        }
        private void SSHSendThread(SSHModel sshModel, string sshPasswd)
        {
            InProgressing.IsActive = true;
            // 在子线程中执行任务
            Thread subThread = new Thread(new ThreadStart(() =>
            {
                string res = GeneralMethod.SendSSHCommand(sshModel.SSHCommand, sshModel.IPAddress, sshModel.SSHPort, sshModel.SSHUser, sshPasswd, sshModel.SSHKeyId, sshModel.SSHKeyIsOpen);
                _dispatcherQueue.TryEnqueue(() =>
                {
                    SSHResponse.Subtitle = res;
                    SSHResponse.IsOpen = true;
                    InProgressing.IsActive = false;
                });
            }));
            subThread.Start();
        }
        private async void EditThisConfig(SSHModel sshModel)
        {
            // 创建一个新的dialog对象
            AddSSH dialog = new AddSSH(sshModel);
            // 对此dialog对象进行配置
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.PrimaryButtonText = resourceLoader.GetString("Change");
            dialog.CloseButtonText = resourceLoader.GetString("Cancel");
            // 默认按钮为PrimaryButton
            dialog.DefaultButton = ContentDialogButton.Primary;

            // 订阅 DataSaved 事件，处理从管理密钥页面返回后保存数据的情况
            dialog.DataSaved += (s, args) => LoadData();

            // 显示Dialog并等待其关闭
            ContentDialogResult result = await dialog.ShowAsync();

            // 如果按下了Primary
            if (result == ContentDialogResult.Primary)
            {
                // 实例化SQLiteHelper
                SQLiteHelper dbHelper = new SQLiteHelper();
                // 更新数据
                dbHelper.UpdateSSHData(sshModel);
                // 重新加载数据
                LoadData();
            }
        }
        private void CopyThisConfig(SSHModel sshModel)
        {
            sshModel.Name = $"{sshModel.Name} Copy";
            SQLiteHelper dbHelper = new SQLiteHelper();
            dbHelper.InsertSSHData(sshModel);
            // 重新加载数据
            LoadData();
        }
        private async void ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            ImportConfig.IsEnabled = false;
            // 实例化SQLiteHelper
            SQLiteHelper dbHelper = new SQLiteHelper();
            // 获取导入的数据
            SSHModel sshModel = await SSHMethod.ImportConfig();
            if (sshModel != null)
            {
                // 插入新数据
                dbHelper.InsertSSHData(sshModel);
                // 重新加载数据
                LoadData();
            }
            ImportConfig.IsEnabled = true;
        }
        private async void ExportConfigFunction(SSHModel sshModel)
        {
            string result = await SSHMethod.ExportConfig(sshModel);
            SaveFileTips.Title = result;
            SaveFileTips.IsOpen = true;
        }
        private void AboutAliPay_Click(object sender, RoutedEventArgs e)
        {
            AboutAliPayTips.IsOpen = true;
        }
        private void AboutWePay_Click(object sender, RoutedEventArgs e)
        {
            AboutWePayTips.IsOpen = true;
        }
        private void OnGridViewRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // 获取右键点击的Item
            FrameworkElement gridViewItem = (sender as FrameworkElement);

            if (gridViewItem != null && InProgressing.IsActive == false)
            {
                // 获取右键点击的数据对象（WoLModel）
                SSHModel selectedItem = gridViewItem?.DataContext as SSHModel;

                // 实例化SQLiteHelper
                SQLiteHelper dbHelper = new SQLiteHelper();

                // 将右键点击的项设置为选中项
                dataGridView.SelectedItem = selectedItem;

                // 创建ContextMenu
                MenuFlyout menuFlyout = new MenuFlyout();

                MenuFlyoutItem sshShutdownPCMenuItem = new MenuFlyoutItem
                {
                    Text = resourceLoader.GetString("RunSSHScript")
                };
                sshShutdownPCMenuItem.Click += (sender, e) =>
                {
                    SSHSend(selectedItem);
                };
                menuFlyout.Items.Add(sshShutdownPCMenuItem);

                // 添加分割线
                MenuFlyoutSeparator separator = new MenuFlyoutSeparator();
                menuFlyout.Items.Add(separator);

                MenuFlyoutItem exportMenuItem = new MenuFlyoutItem
                {
                    Text = resourceLoader.GetString("Export")
                };
                exportMenuItem.Click += (sender, e) =>
                {
                    ExportConfigFunction(selectedItem);
                };
                menuFlyout.Items.Add(exportMenuItem);

                MenuFlyoutItem replaceMenuItem = new MenuFlyoutItem
                {
                    Text = resourceLoader.GetString("Overlay")
                };
                replaceMenuItem.Click += (sender, e) =>
                {
                    // 保存选中的数据对象以便确认后执行
                    selectedSSHModel = selectedItem;
                    // 弹出二次确认Flyout
                    confirmationReplaceFlyout.ShowAt(gridViewItem);
                };
                menuFlyout.Items.Add(replaceMenuItem);

                // 添加分割线
                MenuFlyoutSeparator separator2 = new MenuFlyoutSeparator();
                menuFlyout.Items.Add(separator2);

                if (dbHelper.GetSSHPreRowsId(selectedItem) != -1)
                {
                    MenuFlyoutItem upSwapMenuItem = new MenuFlyoutItem
                    {
                        Text = resourceLoader.GetString("MoveForward")
                    };
                    upSwapMenuItem.Click += (sender, e) =>
                    {
                        // 向上移动
                        if (dbHelper.UpSwapSSHRows(selectedItem))
                        {
                            // 重新加载数据
                            LoadData();
                        }
                    };
                    menuFlyout.Items.Add(upSwapMenuItem);
                }

                if (dbHelper.GetSSHPosRowsId(selectedItem) != -1)
                {
                    MenuFlyoutItem downSwapMenuItem = new MenuFlyoutItem
                    {
                        Text = resourceLoader.GetString("MoveBackward")
                    };
                    downSwapMenuItem.Click += (sender, e) =>
                    {
                        // 向上移动
                        if (dbHelper.DownSwapSSHRows(selectedItem))
                        {
                            // 重新加载数据
                            LoadData();
                        }
                    };
                    menuFlyout.Items.Add(downSwapMenuItem);
                }

                if (dbHelper.GetSSHPreRowsId(selectedItem) != -1 || dbHelper.GetSSHPosRowsId(selectedItem) != -1)
                {
                    // 添加分割线
                    MenuFlyoutSeparator separator3 = new MenuFlyoutSeparator();
                    menuFlyout.Items.Add(separator3);
                }

                MenuFlyoutItem editMenuItem = new MenuFlyoutItem
                {
                    Text = resourceLoader.GetString("Edit")
                };
                editMenuItem.Click += (sender, e) =>
                {
                    EditThisConfig(selectedItem);
                };
                menuFlyout.Items.Add(editMenuItem);

                MenuFlyoutItem copyMenuItem = new MenuFlyoutItem
                {
                    Text = resourceLoader.GetString("Copy")
                };
                copyMenuItem.Click += (sender, e) =>
                {
                    CopyThisConfig(selectedItem);
                };
                menuFlyout.Items.Add(copyMenuItem);

                MenuFlyoutItem deleteMenuItem = new MenuFlyoutItem
                {
                    Text = resourceLoader.GetString("Delete")
                };
                deleteMenuItem.Click += (sender, e) =>
                {
                    // 保存选中的数据对象以便确认后执行
                    selectedSSHModel = selectedItem;
                    // 弹出二次确认Flyout
                    confirmationFlyout.ShowAt(gridViewItem);
                };
                menuFlyout.Items.Add(deleteMenuItem);

                Thread.Sleep(10);

                // 在指定位置显示ContextMenu
                menuFlyout.ShowAt(gridViewItem, e.GetPosition(gridViewItem));
            }

        }
        private void OnGridViewDoubleTapped(object sender, RoutedEventArgs e)
        {
            // 处理左键双击事件的代码
            // 获取右键点击的Item
            FrameworkElement listViewItem = (sender as FrameworkElement);
            if (listViewItem != null && InProgressing.IsActive == false)
            {
                // 获取点击的数据对象
                SSHModel selectedItem = listViewItem?.DataContext as SSHModel;
                SSHSend(selectedItem);
            }
        }
    }
}
