# 打包、安装与环境配置

本文档说明 `autojs6-dev-tools-plus` 的构建、发布、安装与运行环境准备方式。

## 1. 通用前置条件

无论在哪个平台运行，建议先准备好：

- `.NET SDK 8.0.420`（开发构建时）
- `ADB`（`platform-tools`）已加入 `PATH`，或已通过 `ANDROID_SDK_ROOT` / `ANDROID_HOME` 指向 Android SDK
- 至少一台可访问的 Android 真机或模拟器（如果要使用截图与 UI dump 工作流）

建议先执行：

```bash
dotnet --info
adb version
adb devices
```

## 2. 本地开发运行

```bash
dotnet restore autojs6-dev-tools-plus.sln
dotnet build autojs6-dev-tools-plus.sln
dotnet run --project App.Avalonia/App.Avalonia.csproj
```

## 3. 发布命令

### Windows

```bash
dotnet publish App.Avalonia/App.Avalonia.csproj \
  -c Release \
  -r win-x64 \
  --self-contained false \
  -o artifacts/win-x64
```

### macOS

```bash
dotnet publish App.Avalonia/App.Avalonia.csproj \
  -c Release \
  -r osx-x64 \
  --self-contained false \
  -o artifacts/osx-x64
```

### Linux

```bash
dotnet publish App.Avalonia/App.Avalonia.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained false \
  -o artifacts/linux-x64
```

## 4. 各平台说明

### Windows

- 当前仓库已经显式锁定 `OpenCvSharp4.runtime.win`
- 发布产物中应当随包携带 Windows 所需的 OpenCV native runtime
- `adb.exe` 会优先从 `PATH` 或常见 Android SDK 路径中自动发现

建议发布后先做基础检查：

```powershell
Get-ChildItem artifacts/win-x64
.\artifacts\win-x64\App.Avalonia.exe
```

### macOS

- 当前仓库**不会**在 `*.csproj` 中直接锁定 `OpenCvSharp4.runtime.osx.*`
- macOS 的 OpenCV native provisioning 需要在环境准备或发布打包阶段单独处理
- 不要为了“看起来完整”就直接加一个旧版 runtime 包进去

在真正发布 macOS 版本前，至少要确认：

1. `OpenCvSharpExtern` native 库已就位
2. managed / native 版本对已人工验证兼容
3. 已在真实 macOS 主机上通过截图、UI dump、模板匹配 smoke test

### Linux

- Linux 目前仍然是“保留兼容路径”，不是完整交付路径
- 当前已经保留 `linux-x64` 的 publish 入口
- OpenCV native 验证通过额外探测脚本完成，不通过项目依赖图强绑

在 Linux 主机发布后可执行：

```bash
bash scripts/linux/probe-opencv-native.sh artifacts/linux-x64
```

该探测会检查：

- `OpenCvSharp.dll` 是否存在
- `libOpenCvSharpExtern.so` 是否存在
- `ldd` 是否存在 `not found`

## 5. 运行环境检查清单

### 设备工作流检查

- `adb devices` 至少显示一台 `device`
- 设备已解锁
- 设备或模拟器允许当前界面执行 UI dump

### 图像工作流检查

- 可以成功截屏或载入本地截图
- 已存在模板图片
- 如果使用区域匹配，裁剪区域必须有效

### 控件工作流检查

- 当前截图与当前 UI dump 来源一致
- 已成功拉取 UI 树
- 生成控件模式代码前，必须先选中目标控件节点

## 6. 当前 CI 基线

仓库 CI 当前会执行：

1. Restore
2. Build
3. 运行 `Core.Tests`
4. 发布 `win-x64`、`osx-x64`、`linux-x64`

对应工作流文件：`.github/workflows/build.yml`

## 7. 发布注意事项

- 没有明确验证前，不要对外宣称 macOS OpenCV native 已完全闭环
- 不要因为 Linux `dotnet publish` 成功，就宣称 Linux 运行时支持已经完成
- 当前最稳的打包目标仍然是 Windows
