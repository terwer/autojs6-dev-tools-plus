# 开发指南

本文档面向 `autojs6-dev-tools-plus` 的开发者与后续维护者。

## 事实来源

- 依赖关系、包选择、分层边界以项目文件为准。
- 如果本文档与 `*.csproj` 冲突，以项目文件为准，并且必须及时回写本文档。
- 架构依赖方向必须保持：

```text
App.Avalonia -> Infrastructure -> Core
```

## 本地构建与测试

```bash
dotnet restore autojs6-dev-tools-plus.sln
dotnet build autojs6-dev-tools-plus.sln
dotnet test tests/Core.Tests/Core.Tests.csproj
```

## OpenCV native 依赖策略

本仓库**刻意**把 OpenCV 的 managed wrapper 与 native runtime provisioning 分开处理。

### Managed 包基线

- Managed wrapper：`OpenCvSharp4`
- 集成边界：`Infrastructure`
- **禁止**在 `Core` 或 `App.Avalonia` 里直接引用 OpenCV runtime 包

### Windows 策略

当前只有 Windows 在仓库内显式接入了 OpenCV native runtime 包。

- Managed 包：`OpenCvSharp4`
- Native runtime 包：`OpenCvSharp4.runtime.win`
- 该 runtime 包在 `Infrastructure/Infrastructure.csproj` 中声明
- 这是当前仓库里**唯一**被显式锁定的 OpenCV native runtime 包

### macOS 策略

当前 **不会** 在 `*.csproj` 里直接锁定 macOS 的 OpenCV native runtime。

这是有意为之，后续不要“顺手补上”。

#### 原因

- 仓库只锁定了 managed wrapper
- 常见能搜到的 macOS OpenCvSharp runtime 包，与当前锁定的 managed 包存在版本错位风险
- 如果未经验证就把旧版 macOS runtime 包直接加进项目，很容易埋下 ABI / 版本不匹配问题

#### 当前规则

- 仓库内保留 `OpenCvSharp4`
- **不要**把 `OpenCvSharp4.runtime.osx.*` 直接加入项目，除非已经人工验证该版本组合完全兼容
- macOS native 库通过环境准备或发布打包阶段单独 provision，不放进当前仓库的 NuGet 包图里强绑

#### 如果后续要调整该策略

在修改 macOS 策略之前，必须先完成以下验证：

1. managed 包版本与 macOS native runtime 版本确实兼容
2. `OpenCvSharpExtern` 在 macOS 上能被正常加载
3. 模板匹配 smoke test 已在真实 macOS 机器上通过
4. 同步更新 `DEVELOPMENT.md`、`DEVELOPMENT_zh_CN.md` 与 `docs/cross-platform-build.md`

### Linux 策略

当前 Linux 仍然是“架构预留路径”，不是已完成的 native 交付路径。

- 仓库保留 Linux 的 `dotnet publish` 入口
- 仓库当前**不默认锁定** Linux 的 OpenCV native runtime 包
- Linux native 依赖通过“显式探测”来校验，而不是假设天然可用

## Linux native 依赖探测方案

仓库已提供探测脚本：

```text
scripts/linux/probe-opencv-native.sh
```

在 Linux 发布产物生成后执行：

```bash
bash scripts/linux/probe-opencv-native.sh artifacts/linux-x64
```

该脚本会检查：

1. 发布目录里是否存在 `OpenCvSharp.dll`
2. 是否能在发布目录或系统常见库路径中找到 `libOpenCvSharpExtern.so`
3. `ldd` 是否能把 native 依赖链解析完整，没有 `not found`

### 重要说明

探测脚本通过，只能说明 native 依赖链**看起来可加载**。

它**不能替代**真实运行时 smoke test。

## 一定要避免的坑

- 不要把 OpenCV runtime 依赖下沉到 `Core`
- 不要因为 NuGet 能搜到某个 macOS runtime 包，就直接加进来
- 不要因为 Linux `dotnet publish` 成功，就误判 Linux native 支持已经完成
- 不要修改平台 native 打包策略却不更新中英文开发文档
