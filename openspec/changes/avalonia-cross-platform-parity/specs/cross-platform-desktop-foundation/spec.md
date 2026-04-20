## ADDED Requirements

### Requirement: 系统能够提供跨平台桌面应用壳层
系统 SHALL 基于 Avalonia 提供统一的桌面应用壳层，使同一套工作台信息架构可运行于 Windows 与 macOS，并为 Linux 保留兼容入口。

#### Scenario: Windows 启动主工作台
- **WHEN** 用户在 Windows 上启动应用
- **THEN** 系统显示统一的主窗口与工作台布局
- **THEN** 设备列表、画布区域、属性面板、代码预览面板均可正常加载

#### Scenario: macOS 启动主工作台
- **WHEN** 用户在 macOS 上启动应用
- **THEN** 系统显示与 Windows 对齐的主工作台能力
- **THEN** 不因平台差异缺失核心工作流入口

#### Scenario: Linux 保留兼容入口
- **WHEN** 用户在 Linux 环境构建或启动应用
- **THEN** 系统保留桌面生命周期与渲染初始化路径
- **THEN** 不因架构设计缺失而无法继续补齐平台支持

### Requirement: 系统能够抽象平台相关桌面服务
系统 SHALL 抽象文件对话框、剪贴板、快捷键、窗口状态、消息提示等平台相关能力，避免业务逻辑直接依赖操作系统 API。

#### Scenario: 文件保存对话框抽象
- **WHEN** 用户执行导出模板或导出代码操作
- **THEN** 系统通过统一的平台服务接口打开文件保存对话框
- **THEN** 业务层不直接调用平台专有 API

#### Scenario: 剪贴板写入抽象
- **WHEN** 用户执行复制坐标、复制 XPath 或复制代码操作
- **THEN** 系统通过统一剪贴板服务完成写入
- **THEN** Windows 与 macOS 的行为保持一致

#### Scenario: 窗口状态管理抽象
- **WHEN** 应用启动并进入主工作台
- **THEN** 系统通过统一窗口服务处理最大化、恢复与尺寸持久化
- **THEN** 不在业务视图代码中硬编码单平台窗口 API

### Requirement: 系统能够提供跨平台资源与打包基础
系统 SHALL 提供 Windows/macOS 的可分发构建路径，并为 Linux 保留构建与依赖校验设计。

#### Scenario: Windows 构建产物生成
- **WHEN** 运行 Windows 发布流程
- **THEN** 系统生成可分发桌面应用产物
- **THEN** 产物包含运行所需的托管依赖与平台资源

#### Scenario: macOS 构建产物生成
- **WHEN** 运行 macOS 发布流程
- **THEN** 系统生成可分发桌面应用产物
- **THEN** 平台资源、应用图标和基础元数据完整

#### Scenario: Linux 构建校验保留
- **WHEN** 运行 Linux 构建或 CI 校验流程
- **THEN** 系统至少完成编译、依赖检查或 smoke 验证设计中的一项
- **THEN** 后续补齐 Linux 发布时无需推翻架构

### Requirement: 系统能够保持主工作流能力对齐
系统 SHALL 保证跨平台项目的主工作台入口与能力边界对齐现有 Windows 工具，而不是仅提供简化演示版。

#### Scenario: 工作台能力对齐检查
- **WHEN** 用户进入主工作台
- **THEN** 系统提供截图、拉取 UI 树、匹配测试、生成代码、导出模板等核心入口
- **THEN** 不因跨平台迁移而删除关键功能模块
