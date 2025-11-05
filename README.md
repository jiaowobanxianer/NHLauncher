# NHLauncher

**NHLauncher** 是一款基于 **Avalonia UI** 开发的跨平台启动器（Launcher）。  
可与 **LauncherPacker** 和 **LauncherPackerUploadReceiver** 配合使用，实现热更新功能。

---

## 🚀 功能特性

- 🎨 基于 Avalonia UI，支持 **Windows** / ~~Linux~~ / ~~macOS~~  
- 🔄 与 LauncherPacker 配合实现资源与程序的版本更新  
- 🧰 支持差分更新、整包更新（修复）、自更新  
- ⚙️ 可自由配置启动路径与目标程序  
- 📦 多项目支持：可管理多个子游戏或应用  

---

## 📁 项目结构

```text
NHLauncher/
├─ Games/
│ ├─ {ProjectID}/
│ │ ├─ {Platform}/
│ │ │ ├─ MainApp.exe
│ │ │ ├─ ...
│ │ │ └─ manifest.json
│ └─ ...
├─ NHLauncher.exe
├─ updater.json
├─ manifest.json
└─ ...
```

将需要执行的应用放置于以下路径并在启动器中添加即可被识别：

/Games/{ProjectID}/{Platform}/{APPName}

示例：

/Games/MyGame/Windows/MyGame.exe

---

## 🔧 热更新功能使用说明

若要使用 **热更新** 功能，需要在服务端运行以下程序：

### ✅ 服务器端

运行：LauncherPackerUploadReceiver

用于接收和分发更新文件。  
并在服务器的 `appsettings.json` 中配置更新参数（例如 `APIKey`）。

---

### 💻 客户端

客户端的 `LauncherPacker` 目录中需包含配置文件：

LauncherPackerSetting.json

该文件内容应与服务器端 `appsettings.json` 的关键字段 **以及代理路径** 完全一致，  以保证正确通信与版本校验。

⚠️ 启动器自更新使用启动器目录下的 `updater.json` 进行配置，请确保服务器与客户端配置保持一致。

---

## 📤 更新上传说明

自更新与普通更新的上传步骤完全相同：

1. 启动 `LauncherPacker`  
2. 指定目标文件夹  
3. 生成 `Manifest`  
4. 上传至服务器端（由 `LauncherPackerUploadReceiver` 接收）

---

## 🧩 构建与运行

### 依赖项

- .NET 8 SDK 及以上  
- Avalonia 11.x  
- Newtonsoft.Json  

### 构建命令

```
dotnet build
```

### 运行命令

```
dotnet run --project NHLauncher.Desktop
```

---

### ⚖️ 版权与许可

> Copyright © 2025 Jiaowobanxianer  
> Licensed under the GPL-3.0 License.  
> 本项目在 GPL-3.0 协议下开源。  
> 若需用于商业项目，请遵守相关条款。

---