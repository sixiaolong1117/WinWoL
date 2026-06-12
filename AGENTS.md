# AGENTS.md — WinWoL / 远程工具箱

## 项目概述

基于 WinUI3 (Windows App SDK) 的远程工具箱桌面应用。功能：WoL 远程唤醒、RDP 远程桌面、SSH 快捷命令、SSH 密钥管理。通过 Microsoft Store 分发。

## 构建

用 Visual Studio 打开 `WinWoL.sln`，选择配置后构建/调试。

```powershell
# 命令行构建（需要安装 .NET 8 SDK + Windows SDK 10.0.19041）
dotnet build WinWoL.sln -c Debug -p:Platform=x64
```

- 目标框架：`net8.0-windows10.0.19041.0`，最低版本 1809
- 平台：x86 / x64 / ARM64
- 仅 Debug|x64 和 Release|x64 启用 `PublishReadyToRun` + `SelfContained`
- MSIX 打包：`AppxBundle=Always`，`AppxBundlePlatforms` 包含全部三平台
- 无测试项目、无 lint/typecheck 工具、无 CI/CD 配置

## 架构

单项目解决方案，无多包边界。

```
WinWoL/
├── App.xaml.cs              # 单实例入口，窗口大小设置
├── MainWindow.xaml.cs       # NavigationView + Mica/Acrylic 背景
├── Datas/
│   └── SQLiteHelper.cs      # SQLite 数据层（v3 schema），建表/迁移/CRUD
├── Methods/
│   ├── WoLMethod.cs         # WoL MagicPacket(UDP)、Ping、TCPing、RDP、导入导出
│   ├── SSHMethod.cs         # SSH 配置导入导出
│   ├── GeneralMethod.cs     # SSH 命令执行（SSH.NET）
│   ├── SSHKeyMethod.cs      # SSH 密钥导入/解析/元数据提取
│   ├── SSHKeyProtection.cs  # Windows DPAPI 加解密（LOCAL=user）
│   └── WindowsHelloHelper.cs # Windows Hello 生物识别验证
├── Models/                  # WoLModel, SSHModel, SSHKeyModel, SSHPasswdModel
├── Pages/                   # WoL.xaml, SSHShortcut.xaml, About.xaml, SettingsPage.xaml
│   └── Dialogs/             # AddWoL, AddSSH, EnterSSHPasswd, ManageSSHKeys, PingTools
├── Language/                # en-US, zh-CN, ja-JP, ko-KR（.resw 资源文件）
└── Assets/                  # 应用图标、启动画面
```

## 关键约定

- **SSH 密钥存储**：私钥通过 DPAPI (`LOCAL=user`) 加密后存入 SQLite `SSHKeyTable`，不依赖原始文件路径。修改密钥相关逻辑时注意 `SSHKeyProtection.Protect/Unprotect` 是同步阻塞调用。
- **数据库迁移**：`SQLiteHelper.UpgradeDatabase()` 在每次 CRUD 操作前调用，通过 `EnsureColumn` 做增量迁移。`CurrentDatabaseVersion` 常量控制版本号。
- **单实例**：`App.xaml.cs` 通过 `AppInstance.FindOrRegisterForKey` 实现，重复启动会激活已有实例。
- **设置存储**：使用 `Windows.Storage.ApplicationData.Current.LocalSettings`，非 JSON 文件。
- **UI 本地化**：字符串资源在 `Language/<locale>/Resources.resw`，代码中通过 `ResourceLoader` 加载。新增 UI 字符串需在所有四个语言文件中添加。
- **XAML 页面注册**：新页面需在 `.csproj` 中添加 `<Page Update>` 条目（`Generator=MSBuild:Compile`），否则 XAML 不会被编译。
- **配置导入导出**：WoL 用 `.wolconfigx`，SSH 用 `.sshconfigx`，均为 JSON 序列化。

## 无以下工具

无测试框架、无 linter、无 formatter、无 typecheck、无 CI/CD、无 pre-commit hooks。构建验证依赖 Visual Studio 或 `dotnet build`。
