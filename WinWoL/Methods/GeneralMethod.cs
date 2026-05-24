using Renci.SshNet;
using System;
using System.IO;
using System.Text;
using WinWoL.Datas;
using WinWoL.Models;

namespace WinWoL.Methods
{
    public class GeneralMethod
    {
        // SSH执行
        public static string SendSSHCommand(string sshCommand, string sshHost, string sshPort, string sshUser, string sshPasswd, string sshKeyId, string privateKeyIsOpen)
        {
            try
            {
                bool usePrivateKey = string.Equals(privateKeyIsOpen, "True", StringComparison.OrdinalIgnoreCase);
                SshClient sshClient = InitializeSshClient(sshHost, int.Parse(sshPort), sshUser, sshPasswd, sshKeyId, usePrivateKey);

                if (sshClient != null)
                {
                    return ExecuteSshCommand(sshClient, sshCommand);
                }
                else
                {
                    return "SSH 客户端初始化失败。";
                }
            }
            catch (Exception ex)
            {
                return "SSH 操作失败：" + ex.Message;
            }
        }

        // SSH初始化（仅使用数据库中的SSHKeyId，不再回退到旧版文件路径）
        private static SshClient InitializeSshClient(string sshHost, int sshPort, string sshUser, string sshPasswd, string sshKeyId, bool usePrivateKey)
        {
            if (usePrivateKey)
            {
                PrivateKeyFile privateKeyFile = LoadPrivateKeyFile(sshKeyId);
                ConnectionInfo connectionInfo = new ConnectionInfo(sshHost, sshPort, sshUser, new PrivateKeyAuthenticationMethod(sshUser, new PrivateKeyFile[] { privateKeyFile }));
                return new SshClient(connectionInfo);
            }
            else
            {
                return new SshClient(sshHost, sshPort, sshUser, sshPasswd);
            }
        }

        /// <summary>
        /// 根据SSHKeyId从数据库加载私钥
        /// </summary>
        private static PrivateKeyFile LoadPrivateKeyFile(string sshKeyId)
        {
            if (string.IsNullOrWhiteSpace(sshKeyId))
            {
                throw new InvalidOperationException("未配置 SSH 密钥。");
            }

            // SSHKeyId 为纯数字，从数据库加载
            if (int.TryParse(sshKeyId, out int keyId))
            {
                SQLiteHelper dbHelper = new SQLiteHelper();
                SSHKeyModel sshKey = dbHelper.GetSSHKeyById(keyId);
                if (sshKey == null || string.IsNullOrWhiteSpace(sshKey.PrivateKey))
                {
                    throw new InvalidOperationException("未找到可用的 SSH 密钥。");
                }

                MemoryStream privateKeyStream = new MemoryStream(Encoding.UTF8.GetBytes(sshKey.PrivateKey));
                return new PrivateKeyFile(privateKeyStream);
            }

            throw new InvalidOperationException("无法识别的 SSH 密钥格式，请在编辑中重新选择密钥。");
        }

        // SSH返回
        private static string ExecuteSshCommand(SshClient sshClient, string sshCommand)
        {
            try
            {
                sshClient.Connect();

                if (sshClient.IsConnected)
                {
                    SshCommand SSHCommand = sshClient.RunCommand(sshCommand);

                    if (!string.IsNullOrEmpty(SSHCommand.Error))
                    {
                        return "错误：" + SSHCommand.Error;
                    }
                    else
                    {
                        return SSHCommand.Result;
                    }
                }
                return "SSH 命令执行失败。";
            }
            finally
            {
                sshClient.Disconnect();
                sshClient.Dispose();
            }
        }
    }
}
