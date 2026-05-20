using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using WinWoL.Datas;
using WinWoL.Methods;
using WinWoL.Models;
using WinWoL.Pages.Dialogs;

namespace WinWoL.Pages
{
    public sealed partial class WoL : Page
    {
        // 启用本地设置数据
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        ResourceLoader resourceLoader = new ResourceLoader();
        WoLModel selectedWoLModel;
        private DispatcherQueue _dispatcherQueue;
        public WoL()
        {
            this.InitializeComponent();
            // 获取UI线程的DispatcherQueue
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            // 加载数据
            LoadData();
            LoadString();

            PingRefConfig.IsEnabled = false;
        }
        private void LoadString()
        {
            Header.Text = resourceLoader.GetString("WoLHeader");
            WoLResultTips.CloseButtonContent = resourceLoader.GetString("Confirm");
            SSHResponse.CloseButtonContent = resourceLoader.GetString("Confirm");
            SaveFileTips.CloseButtonContent = resourceLoader.GetString("Confirm");

            if (localSettings.Values["HideConfigFlag"] == null || localSettings.Values["HideConfigFlag"] as string == "False")
            {
                HideConfig.Icon = new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uE7B3" };
                HideConfig.IsChecked = true;
            }
            else
            {
                HideConfig.Icon = new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uED1A" };
                HideConfig.IsChecked = false;
            }
        }
        private void LoadData()
        {
            // 实例化SQLiteHelper
            SQLiteHelper dbHelper = new SQLiteHelper();
            // 查询数据
            List<WoLModel> dataList = dbHelper.QueryData();

            // 将数据列表绑定到ListView
            dataListView.ItemsSource = dataList;
        }
        // 添加配置按钮点击
        private async void AddConfigButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建一个初始的WoLModel对象
            WoLModel initialWoLData = new WoLModel();

            // 创建一个新的dialog对象
            AddWoL dialog = new AddWoL(initialWoLData);
            // 对此dialog对象进行配置
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.PrimaryButtonText = resourceLoader.GetString("Add");
            dialog.CloseButtonText = resourceLoader.GetString("Cancel");
            // 默认按钮为PrimaryButton
            dialog.DefaultButton = ContentDialogButton.Primary;

            // 显示Dialog并等待其关闭
            ContentDialogResult result = await dialog.ShowAsync();

            // 如果按下了Primary
            if (result == ContentDialogResult.Primary)
            {
                // 实例化SQLiteHelper
                SQLiteHelper dbHelper = new SQLiteHelper();
                // 插入新数据
                dbHelper.InsertData(initialWoLData);
                // 重新加载数据
                LoadData();
            }
        }
        // 隐藏地址按钮点击
        private void HideConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (localSettings.Values["HideConfigFlag"] == null || localSettings.Values["HideConfigFlag"] as string == "False")
            {
                localSettings.Values["HideConfigFlag"] = "True";
                HideConfig.Icon = new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uED1A" };
                HideConfig.IsChecked = false;
            }
            else
            {
                localSettings.Values["HideConfigFlag"] = "False";
                HideConfig.Icon = new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uE7B3" };
                HideConfig.IsChecked = true;
            }

            LoadConfigData();
        }
        // 导入配置按钮点击
        private async void ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            ImportConfig.IsEnabled = false;
            // 实例化SQLiteHelper
            SQLiteHelper dbHelper = new SQLiteHelper();
            // 获取导入的数据
            WoLModel woLModel = await WoLMethod.ImportConfig();
            if (woLModel != null)
            {
                // 插入新数据
                dbHelper.InsertData(woLModel);
                // 重新加载数据
                LoadData();
            }
            ImportConfig.IsEnabled = true;
        }
        private void WoLPC(WoLModel woLModel)
        {
            InProgressing.IsActive = true;
            // 在子线程中执行任务
            Thread subThread = new Thread(new ThreadStart(() =>
            {
                string SuccessFlag = "0";
                // 尝试发送Magic Packet，成功打开已发送弹窗
                try
                {
                    IPAddress wolAddress = WoLMethod.DomainToIp(woLModel.WoLAddress, "IPv4");
                    WoLMethod.sendMagicPacket(woLModel.MacAddress, wolAddress, int.Parse(woLModel.WoLPort));
                    SuccessFlag = "1";
                }
                // 失败打开发送失败弹窗
                catch
                {
                    SuccessFlag = "0";
                }
                if (SuccessFlag == "1")
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        WoLResultTips.IsOpen = true;
                        WoLResultTips.Title = resourceLoader.GetString("MagicPacketSentSuccessfully");
                        WoLResultTips.Subtitle = resourceLoader.GetString("MagicPacketSentSuccessfullySubtitle");
                    });
                }
                else
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        WoLResultTips.IsOpen = true;
                        WoLResultTips.Title = resourceLoader.GetString("MagicPacketSendingFailed");
                        WoLResultTips.Subtitle = resourceLoader.GetString("MagicPacketSendingFailedSubtitle");
                    });
                }
                _dispatcherQueue.TryEnqueue(() =>
                {
                    InProgressing.IsActive = false;
                });
            }));
            subThread.Start();
        }
        private async void EditThisConfig(WoLModel woLModel)
        {
            // 创建一个新的dialog对象
            AddWoL dialog = new AddWoL(woLModel);
            // 对此dialog对象进行配置
            dialog.XamlRoot = this.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.PrimaryButtonText = resourceLoader.GetString("Change");
            dialog.CloseButtonText = resourceLoader.GetString("Cancel");
            // 默认按钮为PrimaryButton
            dialog.DefaultButton = ContentDialogButton.Primary;

            // 显示Dialog并等待其关闭
            ContentDialogResult result = await dialog.ShowAsync();

            // 如果按下了Primary
            if (result == ContentDialogResult.Primary)
            {
                // 实例化SQLiteHelper
                SQLiteHelper dbHelper = new SQLiteHelper();
                // 更新数据
                dbHelper.UpdateData(woLModel);
                // 重新加载数据
                LoadData();
            }
        }
        private async void ExportConfigFunction(WoLModel wolModel)
        {
            string result = await WoLMethod.ExportConfig(wolModel);
            SaveFileTips.Title = result;
            SaveFileTips.IsOpen = true;
        }
        private void CopyThisConfig(WoLModel wolModel)
        {
            wolModel.Name = $"{wolModel.Name} Copy";
            SQLiteHelper dbHelper = new SQLiteHelper();
            dbHelper.InsertData(wolModel);
            // 重新加载数据
            LoadData();
        }
        private async void SSHShutdownConfig(WoLModel wolModel)
        {
            string sshPasswd = null;
            // 使用密码登录
            if (wolModel.SSHKeyIsOpen != "True")
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
                    SSHSendThread(wolModel, sshPasswd);
                }
            }
            else
            {
                sshPasswd = null;
                SSHSendThread(wolModel, sshPasswd);
            }
        }
        private void SSHSendThread(WoLModel wolModel, string sshPasswd)
        {
            InProgressing.IsActive = true;
            // 在子线程中执行任务
            Thread subThread = new Thread(new ThreadStart(() =>
            {
                string res = GeneralMethod.SendSSHCommand(wolModel.SSHCommand, wolModel.IPAddress, wolModel.SSHPort, wolModel.SSHUser, sshPasswd, wolModel.SSHKeyId, wolModel.SSHKeyIsOpen);
                _dispatcherQueue.TryEnqueue(() =>
                {
                    SSHResponse.Subtitle = res;
                    SSHResponse.IsOpen = true;
                    InProgressing.IsActive = false;
                });
            }));
            subThread.Start();
        }
        private async void ConfirmReplace_Click(object sender, RoutedEventArgs e)
        {
            // 关闭二次确认Flyout
            confirmationReplaceFlyout.Hide();
            ImportConfig.IsEnabled = false;
            // 实例化SQLiteHelper
            SQLiteHelper dbHelper = new SQLiteHelper();

            // 获取导入的数据
            WoLModel newModel = await WoLMethod.ImportConfig();

            if (newModel != null)
            {
                // 获取当前配置的ID
                int id = selectedWoLModel.Id;
                // 赋给导入的配置
                newModel.Id = id;
                // 插入新数据
                dbHelper.UpdateData(newModel);
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
            dbHelper.DeleteData(selectedWoLModel);
            // 重新加载数据
            LoadData();
        }
        private void ConfirmShutdown_Click(object sender, RoutedEventArgs e)
        {
            // 关闭二次确认Flyout
            confirmationShutdownFlyout.Hide();
            SSHShutdownConfig(selectedWoLModel);

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
        private void CancelShutdown_Click(object sender, RoutedEventArgs e)
        {
            // 关闭二次确认Flyout
            confirmationShutdownFlyout.Hide();
        }
        private void AboutAliPay_Click(object sender, RoutedEventArgs e)
        {
            AboutAliPayTips.IsOpen = true;
        }
        private void AboutWePay_Click(object sender, RoutedEventArgs e)
        {
            AboutWePayTips.IsOpen = true;
        }
        private async void PingRefConfig_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前选择的项
            WoLModel wolModel = (WoLModel)dataListView.SelectedItem;
            if (wolModel != null)
            {
                // 创建一个新的dialog对象
                PingTools dialog = new PingTools(wolModel);
                // 对此dialog对象进行配置
                dialog.XamlRoot = this.XamlRoot;
                dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
                dialog.CloseButtonText = resourceLoader.GetString("Cancel");
                // 默认按钮为PrimaryButton
                dialog.DefaultButton = ContentDialogButton.Primary;

                // 显示Dialog并等待其关闭
                ContentDialogResult result = await dialog.ShowAsync();

                // 如果按下了Primary
                if (result == ContentDialogResult.Primary)
                {

                }
            }
        }
        private void LoadConfigData()
        {
            if (dataListView.SelectedItem != null)
            {
                // 获取当前选择的项
                WoLModel selectedItem = (WoLModel)dataListView.SelectedItem;

                // 实例化SQLiteHelper
                SQLiteHelper dbHelper = new SQLiteHelper();
                // 查询数据
                List<WoLModel> dataList = null;
                if (localSettings.Values["HideConfigFlag"] == null || localSettings.Values["HideConfigFlag"] as string == "False")
                {
                    dataList = dbHelper.GetDataListById(selectedItem.Id);
                }
                else
                {
                    dataList = dbHelper.GetDataListByIdHideAddress(selectedItem.Id);
                }

                // 将数据列表绑定到ListView
                dataListView2.ItemsSource = dataList;

                if (selectedItem.IPAddress == null || selectedItem.IPAddress == "")
                {
                    PingRefConfig.IsEnabled = false;
                }
                else
                {
                    PingRefConfig.IsEnabled = true;
                }
            }
        }
        private void dataListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadConfigData();
        }
        private void OnListViewRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // 获取右键点击的ListViewItem
            FrameworkElement listViewItem = (sender as FrameworkElement);

            if (listViewItem != null && InProgressing.IsActive == false)
            {
                // 获取右键点击的数据对象（WoLModel）
                WoLModel selectedItem = listViewItem?.DataContext as WoLModel;

                // 实例化SQLiteHelper
                SQLiteHelper dbHelper = new SQLiteHelper();

                // 将右键点击的项设置为选中项
                dataListView.SelectedItem = selectedItem;

                // 创建ContextMenu
                MenuFlyout menuFlyout = new MenuFlyout();

                if (selectedItem.WoLIsOpen == "True")
                {
                    MenuFlyoutItem wolPCMenuItem = new MenuFlyoutItem
                    {
                        Text = resourceLoader.GetString("WoL")
                    };
                    wolPCMenuItem.Click += (sender, e) =>
                    {
                        WoLPC(selectedItem);
                    };
                    menuFlyout.Items.Add(wolPCMenuItem);
                }

                if (selectedItem.RDPIsOpen == "True")
                {
                    MenuFlyoutItem rdpPCMenuItem = new MenuFlyoutItem
                    {
                        Text = resourceLoader.GetString("RDP")
                    };
                    rdpPCMenuItem.Click += (sender, e) =>
                    {
                        string mstscCMD = $"mstsc /v:{selectedItem.IPAddress}:{selectedItem.RDPPort};";
                        WoLMethod.RDPConnect(mstscCMD);
                    };
                    menuFlyout.Items.Add(rdpPCMenuItem);
                }

                if (selectedItem.SSHIsOpen == "True")
                {
                    MenuFlyoutItem sshShutdownPCMenuItem = new MenuFlyoutItem
                    {
                        Text = resourceLoader.GetString("SSHShutdown")
                    };
                    sshShutdownPCMenuItem.Click += (sender, e) =>
                    {
                        // 保存选中的数据对象以便确认后执行
                        selectedWoLModel = selectedItem;
                        // 弹出二次确认Flyout
                        confirmationShutdownFlyout.ShowAt(listViewItem);
                    };
                    menuFlyout.Items.Add(sshShutdownPCMenuItem);
                }

                if (selectedItem.WoLIsOpen == "True" || selectedItem.RDPIsOpen == "True" || selectedItem.SSHIsOpen == "True")
                {
                    // 添加分割线
                    MenuFlyoutSeparator separator = new MenuFlyoutSeparator();
                    menuFlyout.Items.Add(separator);
                }

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
                    selectedWoLModel = selectedItem;
                    // 弹出二次确认Flyout
                    confirmationReplaceFlyout.ShowAt(listViewItem);
                };
                menuFlyout.Items.Add(replaceMenuItem);

                // 添加分割线
                MenuFlyoutSeparator separator2 = new MenuFlyoutSeparator();
                menuFlyout.Items.Add(separator2);

                if (dbHelper.GetPreRowsId(selectedItem) != -1)
                {
                    MenuFlyoutItem upSwapMenuItem = new MenuFlyoutItem
                    {
                        Text = resourceLoader.GetString("MoveForward")
                    };
                    upSwapMenuItem.Click += (sender, e) =>
                    {
                        // 向上移动
                        if (dbHelper.UpSwapRows(selectedItem))
                        {
                            // 重新加载数据
                            LoadData();
                        }
                    };
                    menuFlyout.Items.Add(upSwapMenuItem);
                }

                if (dbHelper.GetPosRowsId(selectedItem) != -1)
                {
                    MenuFlyoutItem downSwapMenuItem = new MenuFlyoutItem
                    {
                        Text = resourceLoader.GetString("MoveBackward")
                    };
                    downSwapMenuItem.Click += (sender, e) =>
                    {
                        // 向上移动
                        if (dbHelper.DownSwapRows(selectedItem))
                        {
                            // 重新加载数据
                            LoadData();
                        }
                    };
                    menuFlyout.Items.Add(downSwapMenuItem);
                }

                if (dbHelper.GetPreRowsId(selectedItem) != -1 || dbHelper.GetPosRowsId(selectedItem) != -1)
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
                    selectedWoLModel = selectedItem;
                    // 弹出二次确认Flyout
                    confirmationFlyout.ShowAt(listViewItem);
                };
                menuFlyout.Items.Add(deleteMenuItem);

                Thread.Sleep(10);

                // 在指定位置显示ContextMenu
                menuFlyout.ShowAt(listViewItem, e.GetPosition(listViewItem));
            }
        }
        private void OnListViewDoubleTapped(object sender, RoutedEventArgs e)
        {
            // 处理左键双击事件的代码
            // 获取右键点击的ListViewItem
            FrameworkElement listViewItem = (sender as FrameworkElement);
            if (listViewItem != null && InProgressing.IsActive == false)
            {
                // 获取点击的数据对象
                WoLModel selectedItem = listViewItem?.DataContext as WoLModel;
                WoLPC(selectedItem);
            }
        }
    }
}
