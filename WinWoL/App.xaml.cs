using Microsoft.UI.Xaml;
using System;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinWoL
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
        }
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // 获取当前应用实例
            var currentInstance = AppInstance.GetCurrent();

            // 注册或查找已经运行的实例
            var instance = AppInstance.FindOrRegisterForKey("WinWoLAppInstance-F*^s2XXpfcIx&%ujYu#Ku*PuGRGZ9m%D");

            // 如果已经有实例在运行，转移激活到已有实例
            if (!instance.IsCurrent)
            {
                // 将激活转移到已有实例
                await instance.RedirectActivationToAsync(currentInstance.GetActivatedEventArgs());

                // 从 resources.resw 中获取窗口标题
                var resourceLoader = new ResourceLoader();
                string appTitle = resourceLoader.GetString("AppTitle"); // 动态加载 AppTitle 资源

                // 确保已激活的实例获得焦点并显示在前台
                HWND existingHwnd = PInvoke.FindWindow(null, appTitle);  // 使用从资源文件加载的窗口标题查找窗口句柄
                if (existingHwnd != default)
                {
                    // 显示窗口并设置为前台
                    PInvoke.ShowWindow(existingHwnd, SHOW_WINDOW_CMD.SW_RESTORE); // 如果窗口最小化，则恢复
                    PInvoke.SetForegroundWindow(existingHwnd); // 将窗口设为前台
                }

                // 退出当前实例
                Environment.Exit(0);
            }

            m_window = new MainWindow();

            HWND hwnd = (HWND)WinRT.Interop.WindowNative.GetWindowHandle(m_window);
            SetWindowSize(hwnd, 700, 500);

            m_window.Activate();
        }
        private void SetWindowSize(HWND hwnd, int width, int height)
        {
            var dpi = PInvoke.GetDpiForWindow(hwnd);
            float scalingFactor = (float)dpi / 96;
            width = (int)(width * scalingFactor);
            height = (int)(height * scalingFactor);

            PInvoke.SetWindowPos(hwnd, (HWND)IntPtr.Zero,
                                0, 0, width, height,
                                SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
        }
        public static Window m_window;
    }
}
