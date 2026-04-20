## ADDED Requirements

### Requirement: 系统能够拉取并解析 Android UI 树
系统 SHALL 通过 AdvancedSharpAdbClient 底层 API 拉取当前界面的 UI dump 数据，并解析为结构化控件节点树。

#### Scenario: 成功拉取并解析 UI 树
- **WHEN** 用户点击“拉取 UI 树”按钮
- **THEN** 系统调用 `DeviceClient.DumpScreenAsync()` 获取 XML 数据
- **THEN** 解析 XML 并构建控件节点树
- **THEN** TreeView 显示控件层级结构

#### Scenario: XML 格式异常容错解析
- **WHEN** UI dump XML 包含无效节点或缺失属性
- **THEN** 系统跳过无效节点并记录警告日志
- **THEN** 继续解析有效节点

### Requirement: 系统能够过滤冗余布局容器节点
系统 SHALL 忽略无业务特征的布局容器，仅提取最底层具备特征的控件节点。

#### Scenario: 过滤无特征布局容器
- **WHEN** 节点 class 包含 `Layout` 且无 clickable、text、content-desc 属性
- **THEN** 系统跳过该节点
- **THEN** 继续提取其子节点

#### Scenario: 保留可交互布局容器
- **WHEN** 布局节点具有 clickable 或文本等业务属性
- **THEN** 系统保留该节点
- **THEN** 将其显示为有效控件

### Requirement: 系统能够精准解析控件属性与 bounds
系统 SHALL 解析每个控件节点的 resource-id、text、content-desc、class、clickable、bounds、package 等属性，并保持左上角原点坐标体系。

#### Scenario: 解析 bounds 坐标
- **WHEN** 控件节点包含 `bounds="[100,200][400,600]"`
- **THEN** 系统解析为 `Rect(100, 200, 300, 400)`
- **THEN** 结果与截图像素坐标直接对齐

### Requirement: 系统能够渲染控件边界框并按类型着色
系统 SHALL 在 Overlay 图层绘制控件边界框，并按控件类型着色。

#### Scenario: 绘制所有控件边界框
- **WHEN** UI 树解析完成
- **THEN** 系统在画布上绘制所有控件边界框
- **THEN** 边界框位置与截图坐标系对齐

#### Scenario: 按类型着色
- **WHEN** 控件为文本、按钮或图像类型
- **THEN** 系统按预定义颜色区分显示
- **THEN** 用户可以通过图例快速辨识类型

### Requirement: 系统能够实现 TreeView 与画布的双向联动
系统 SHALL 支持 TreeView 点击节点高亮画布控件框，以及画布点击控件框自动展开并定位 TreeView 节点。

#### Scenario: TreeView 点击联动画布
- **WHEN** 用户在 TreeView 中点击某个控件节点
- **THEN** 画布高亮显示对应控件边界框
- **THEN** 视口自动滚动至目标位置

#### Scenario: 画布点击联动 TreeView
- **WHEN** 用户在画布上点击某个控件边界框
- **THEN** TreeView 自动展开并定位到对应节点
- **THEN** 节点高亮显示

### Requirement: 系统能够显示控件属性面板
系统 SHALL 在选中控件节点时显示完整属性面板，并支持复制坐标、文本或 XPath 表达式。

#### Scenario: 一键复制坐标
- **WHEN** 用户点击“复制坐标”按钮
- **THEN** 系统将控件 bounds 信息写入剪贴板
- **THEN** 输出格式保持可直接复用

#### Scenario: 一键复制 XPath
- **WHEN** 用户点击“复制 XPath”按钮
- **THEN** 系统生成并复制对应 XPath 表达式
- **THEN** 不要求用户手工拼接路径

### Requirement: 系统能够异步解析大规模 UI 树避免 UI 阻塞
系统 SHALL 支持 5000+ 节点 UI 树的异步解析和虚拟化展示。

#### Scenario: 大规模节点树渲染
- **WHEN** UI 树包含 5000+ 节点
- **THEN** TreeView 使用虚拟化或等价机制仅渲染可见节点
- **THEN** 滚动保持流畅
