## Why

现有 `autojs6-dev-tools` 基于 WinUI 3 + Win2D，仅能在 Windows 上运行，导致使用 macOS 的协作者无法参与同一套可视化开发工作流，也把未来 Linux 支持挡在架构之外。需要单独启动 `autojs6-dev-tools-plus`，在不破坏现有 Windows 工具节奏的前提下，用 Avalonia 重建跨平台桌面壳层，并保持与现有工具的功能对齐。

现在启动这项提案是合适时机，因为当前 Windows 版仍处于建设期，功能边界、双核独立架构、AutoJS6 代码生成约束都还清晰，便于在新项目中按同一能力模型重建，而不是等 WinUI 实现继续加深后再被动迁移。

## What Changes

- 新增 `autojs6-dev-tools-plus` 独立项目，采用 Avalonia 构建跨平台桌面应用，首版交付 Windows/macOS，Linux 保留在架构、构建、打包设计内。
- 保持与 `autojs6-dev-tools` 的能力对齐：ADB 设备管理、截图拉取、交互式裁剪、坐标拾取、UI 树解析、控件边界框渲染、实时匹配测试、AutoJS6 代码生成。
- 保留并强化现有架构原则：图像处理引擎与 UI 图层分析引擎严格解耦，依赖方向保持 `App → Infrastructure → Core ← Infrastructure`。
- 用 Avalonia 自定义画布/渲染层替代 WinUI 3 + Win2D 专属实现，确保同一工作流能在 Windows/macOS 上一致运行，并为 Linux 渲染与打包留出接口与验证面。
- 沿用现有业务约束：AdvancedSharpAdbClient 底层 API、AutoJS6 Rhino 约束、OOM 预防、region 优先、路径正斜杠、坐标左上角原点、双路径代码生成严格独立。
- 明确新旧项目关系：`autojs6-dev-tools` 继续承担 Windows 原型与业务验证，`autojs6-dev-tools-plus` 负责跨平台产品化落地，不对旧项目做就地重写。

## Capabilities

### New Capabilities
- `cross-platform-desktop-foundation`: Avalonia 跨平台桌面壳层、平台服务抽象、跨平台文件/剪贴板/快捷键/打包基础。
- `adb-device-management`: 基于 AdvancedSharpAdbClient 的设备扫描、选择、USB/TCP/IP 连接、日志与异步操作。
- `image-processing-engine`: 截图拉取、位图加载、交互式裁剪、像素坐标拾取、模板导出与图像层缓存。
- `ui-layer-analysis-engine`: UI dump 拉取与解析、布局容器过滤、控件边界框渲染、树与画布双向联动、属性面板。
- `canvas-interaction`: 缩放、平移、旋转、模式切换、状态栏反馈、双图层叠加与高帧率交互。
- `autojs6-code-generator`: 图像模式与控件模式代码生成、Rhino 约束控制、路径处理、预览编辑导出。
- `realtime-match-testing`: 模板匹配实时预览、阈值调节、匹配报告、选择器验证、坐标对齐检查、批量测试。

### Modified Capabilities
<!-- 无现有能力需要修改 -->

## Impact

- 新增独立仓库结构：`App.Avalonia/`、`Core/`、`Infrastructure/`、`tests/`、`openspec/`。
- 新增桌面 UI 依赖：Avalonia 11.x（含桌面生命周期、输入系统、渲染管线、剪贴板/文件对话框抽象）。
- 保持核心技术方向：.NET 8、CommunityToolkit.Mvvm、AdvancedSharpAdbClient、SixLabors.ImageSharp、跨平台 OpenCV 绑定。
- 影响现有能力映射方式：WinUI/Win2D 专属窗口、位图、输入、快捷键、渲染缓存逻辑需要在 Plus 项目中以 Avalonia 方案重建。
- 需要建立跨平台验证矩阵：Windows 10/11、macOS（Apple Silicon/Intel 至少一类）、Linux 预留构建与依赖校验。
- 参考基线来自现有项目与业务资料：
  - `../autojs6-dev-tools/AGENTS.md`
  - `../autojs6-dev-tools/openspec/project.md`
  - `../autojs6-dev-tools/openspec/changes/winui3-visual-dev-toolkit/*`
  - `yxs-day-task` 现有脚本与 AutoJS6 文档/源码
