# AGENTS.md — WinWoL / 远程工具箱

WinUI3 (Windows App SDK) 远程桌面工具：WoL 唤醒、RDP、SSH 命令、SSH 密钥管理。Microsoft Store 分发。

## 构建与测试

```powershell
# 构建（需要 .NET 8 SDK + Windows SDK 10.0.19041）
dotnet build WinWoL.sln -c Debug -p:Platform=x64

# 运行测试（必须指定 Platform=x64，否则 WinAppSDK self-contained 报错）
dotnet test WinWoL.sln -c Debug -p:Platform=x64
```

- 目标 `net8.0-windows10.0.19041.0`，最低 1809
- 平台 x86/x64/ARM64；Debug|x64 和 Release|x64 启用 `PublishReadyToRun` + `SelfContained`
- MSIX 打包三平台，`AppxBundle=Always`
- 无 lint/typecheck/formatter/CI/CD

## 架构

单项目 `WinWoL/` + 测试项目 `WinWoL.Tests/`。

| 目录 | 职责 |
|---|---|
| `Datas/SQLiteHelper.cs` | SQLite 数据层（v3），建表/增量迁移/CRUD/行排序 |
| `Methods/WoLMethod.cs` | MagicPacket 构建 (`BuildMagicPacket`) + 发送、Ping、TCPing、RDP、配置导入导出 |
| `Methods/GeneralMethod.cs` | SSH 命令执行（SSH.NET） |
| `Methods/SSHKeyMethod.cs` | SSH 私钥导入/SSH.NET 元数据提取 |
| `Methods/SSHKeyProtection.cs` | Windows DPAPI 加解密（`LOCAL=user`），同步阻塞 |
| `Methods/SSHMethod.cs` | SSH 配置 `.sshconfigx` 导入导出 |
| `Models/` | WoLModel, SSHModel, SSHKeyModel, SSHPasswdModel |
| `Language/<locale>/Resources.resw` | 四语言（en-US/zh-CN/ja-JP/ko-KR），新增字符串需同步更新 |

## 关键约定

- **SSH 密钥**：私钥通过 DPAPI 加密后存入 SQLite，不依赖文件路径。`SSHKeyProtection.Protect/Unprotect` 是同步阻塞调用。
- **数据库迁移**：`SQLiteHelper.UpgradeDatabase()` 在每次 CRUD 前调用，通过 `PRAGMA table_info` + `EnsureColumn` 增量加列。`CurrentDatabaseVersion` 控制版本号。
- **单实例**：`App.xaml.cs` 用 `AppInstance.FindOrRegisterForKey` 实现；重复启动激活已有实例。
- **设置存储**：`Windows.Storage.ApplicationData.Current.LocalSettings`，非 JSON 文件。
- **XAML 页面注册**：新页面必须在 `.csproj` 中添加 `<Page Update>` + `Generator=MSBuild:Compile`。
- **配置格式**：WoL 导出 `.wolconfigx`，SSH 导出 `.sshconfigx`，均为 Newtonsoft.Json 序列化。

## 测试约定

- xUnit 框架，无 Moq/起测试替身。测试 `SQLiteHelper` 时使用临时文件（`Path.GetTempPath()\wwtest_{guid}.db`），`Dispose()` 中 `ClearAllPools()` + `Delete()`。
- `SQLiteHelper` 构造函数重载 `new SQLiteHelper("Data Source=:memory:")` 或 `new SQLiteHelper("Data Source=temp.db")`。无参构造默认 `wol.db`。
- `WoLMethod` 是 `internal class`，通过 `Properties/AssemblyInfo.cs` 中 `[InternalsVisibleTo("WinWoL.Tests")]` 暴露给测试项目。
- 不测试 UI 页面（需 WinAppDriver）、网络操作（Ping/TCPing/SSH）、FilePicker 导入导出、Windows Hello。
