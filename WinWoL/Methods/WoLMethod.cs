using Newtonsoft.Json;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using WinWoL.Models;

namespace WinWoL.Methods
{
    class WoLMethod
    {
        // 获取域名对应的IP
        public static IPAddress DomainToIp(string domain, string ipVersion)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(domain, out ipAddress))
            {
                // 是IP
                if ((ipVersion == "IPv4" && ipAddress.AddressFamily == AddressFamily.InterNetwork) ||
                    (ipVersion == "IPv6" && ipAddress.AddressFamily == AddressFamily.InterNetworkV6))
                {
                    return ipAddress;
                }
                else
                {
                    throw new ArgumentException("IP version mismatch");
                }
            }
            else
            {
                // 是域名或其他输入
                IPAddress[] addressList = Dns.GetHostEntry(domain).AddressList;

                if (ipVersion == "IPv4")
                {
                    foreach (IPAddress address in addressList)
                    {
                        if (address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return address;
                        }
                    }
                }
                else if (ipVersion == "IPv6")
                {
                    foreach (IPAddress address in addressList)
                    {
                        if (address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            return address;
                        }
                    }
                }

                throw new ArgumentException("No matching IP address found");
            }
        }
        // 构建 MagicPacket 数据包
        public static byte[] BuildMagicPacket(string macAddress)
        {
            string[] hexStrings = macAddress.Split(':');
            byte[] mac = new byte[hexStrings.Length];
            for (int i = 0; i < hexStrings.Length; i++)
                mac[i] = Convert.ToByte(hexStrings[i], 16);

            byte[] packet = new byte[17 * 6];
            for (int i = 0; i < 6; i++)
                packet[i] = 0xFF;
            for (int i = 1; i <= 16; i++)
                for (int j = 0; j < 6; j++)
                    packet[i * 6 + j] = mac[j];

            return packet;
        }

        // 以UDP协议发送MagicPacket
        public static void sendMagicPacket(string macAddress, IPAddress ipAddress, int port)
        {
            byte[] packet = BuildMagicPacket(macAddress);
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.EnableBroadcast = true;

            for (int i = 0; i < 10; i++)
                socket.SendTo(packet, new IPEndPoint(ipAddress, port));

            socket.Close();
        }
        // 导出配置
        public static async Task<string> ExportConfig(WoLModel woLModel)
        {
            // 创建一个FileSavePicker
            FileSavePicker savePicker = new FileSavePicker();
            // 获取当前窗口句柄 (HWND) 
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            // 使用窗口句柄 (HWND) 初始化FileSavePicker
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

            // 为FilePicker设置选项
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            // 用户可以将文件另存为的文件类型下拉列表
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".wolconfigx" });
            // 如果用户没有选择文件类型，则默认为
            savePicker.DefaultFileExtension = ".wolconfigx";

            // 默认文件名
            savePicker.SuggestedFileName = woLModel.Name + "_BackUp_" + DateTime.Now.ToString();

            // 打开Picker供用户选择文件
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    // 阻止更新文件的远程版本，直到我们完成更改并调用 CompleteUpdatesAsync。
                    CachedFileManager.DeferUpdates(file);
                }
                catch
                {
                    // 当您保存至OneDrive等同步盘目录时，在Windows11上可能引起DeferUpdates错误，备份文件不一定写入正确。
                    return "保存行为完成，但当您保存至OneDrive等同步盘目录时，在Windows11上可能引起DeferUpdates错误，备份文件不一定写入正确。";
                }

                // 将数据序列化为 JSON 格式
                string jsonData = JsonConvert.SerializeObject(woLModel);

                // 写入文件
                await FileIO.WriteTextAsync(file, jsonData);

                // 让Windows知道我们已完成文件更改，以便其他应用程序可以更新文件的远程版本。
                // 完成更新可能需要Windows请求用户输入。
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status == FileUpdateStatus.Complete)
                {
                    // 保存成功
                    return "保存成功";
                }
                else if (status == FileUpdateStatus.CompleteAndRenamed)
                {
                    // 重命名并保存成功
                    return "重命名并保存成功";
                }
                else
                {
                    // 文件无法保存！
                    return "无法保存！";
                }
            }
            return "错误！";
        }
        // 导入配置
        public static async Task<WoLModel> ImportConfig()
        {
            // 创建一个FileOpenPicker
            var openPicker = new FileOpenPicker();
            // 获取当前窗口句柄 (HWND) 
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            // 使用窗口句柄 (HWND) 初始化FileOpenPicker
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // 为FilePicker设置选项
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            // 建议打开位置 桌面
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            // 文件类型过滤器
            openPicker.FileTypeFilter.Add(".wolconfigx");

            // 打开选择器供用户选择文件
            var file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                // 读取 JSON 文件内容
                string jsonData = await FileIO.ReadTextAsync(file);
                // 反序列化JSON数据为WoLModel对象
                WoLModel importedData = JsonConvert.DeserializeObject<WoLModel>(jsonData);
                if (importedData != null)
                {
                    // 成功导入配置数据。 
                    return importedData;
                }
                else
                {
                    // JSON数据无法反序列化为配置数据。 
                    return null;
                }
            }
            else
            {
                // 未选择JSON文件。
                return null;
            }
        }
        // 唤起RDP
        public static void RDPConnect(string arguments)
        {
            // 创建一个新的进程
            Process process = new Process();
            // 指定运行PowerShell
            process.StartInfo.FileName = "PowerShell.exe";
            // 参数为唤起mstsc的参数
            // 他保存在localSettings中，随主刷新函数刷新
            process.StartInfo.Arguments = arguments;
            //是否使用操作系统shell启动
            process.StartInfo.UseShellExecute = false;
            //是否在新窗口中启动该进程的值 (不显示程序窗口)
            process.StartInfo.CreateNoWindow = true;
            // 进程开始
            process.Start();
            // 等待执行结束
            process.WaitForExit();
            // 进程关闭
            process.Close();
        }
        // Ping测试
        public static string PingTest(string domain)
        {
            // 获取IP地址
            // 在这里执行这个操作，可以处理一些非法IP的输入问题（例如：广播地址 255.255.255.255）
            // 非法IP会被返回“主机地址无法联通”，而不会让pingSender报错导致应用崩溃
            IPAddress ipAddress = DomainToIp(domain, "IPv4");

            // Ping实例对象
            Ping pingSender = new Ping();
            // Ping选项
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            string data = "ping";
            byte[] buf = Encoding.ASCII.GetBytes(data);
            // 调用同步Send方法发送数据，结果存入reply对象;
            PingReply reply = pingSender.Send(ipAddress, 4000, buf, options);

            // 判断replay，是否连通
            if (reply.Status == IPStatus.Success)
            {
                return $"{reply.RoundtripTime} ms";
            }
            else
            {
                return $"{reply.Status}";
            }
        }
        // Tcping测试
        public static long TCPingTest(string domain, string port)
        {
            // 获取IP地址
            // 在这里执行这个操作，可以处理一些非法IP的输入问题（例如：广播地址 255.255.255.255）
            // 非法IP会被返回“主机地址无法联通”，而不会让pingSender报错导致应用崩溃
            IPAddress ipAddress = DomainToIp(domain, "IPv4");
            int iPort = int.Parse(port);

            using (TcpClient client = new TcpClient())
            {
                client.ReceiveTimeout = 2000;
                client.SendTimeout = 2000;

                Stopwatch stopwatch = new Stopwatch();

                try
                {
                    stopwatch.Start();
                    client.Connect(ipAddress, iPort);
                    stopwatch.Stop();
                    return stopwatch.ElapsedMilliseconds;
                }
                catch (SocketException)
                {
                    // 连接失败
                    return -1;
                }
            }
        }
    }
}
