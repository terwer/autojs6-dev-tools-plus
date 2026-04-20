## ADDED Requirements

### Requirement: 系统能够扫描并列出所有已连接的 ADB 设备
系统 SHALL 通过 AdvancedSharpAdbClient 底层 API 扫描所有已连接的 Android 设备，并在 UI 中显示设备列表，包含设备序列号、型号、状态信息。

#### Scenario: 成功扫描到多个设备
- **WHEN** 用户点击“刷新设备”按钮
- **THEN** 系统调用 `AdbClient.GetDevices()` 获取设备列表
- **THEN** UI 显示所有设备的序列号、型号、连接状态

#### Scenario: 未检测到任何设备
- **WHEN** 用户点击“刷新设备”按钮且无设备连接
- **THEN** 系统显示“未检测到设备”提示
- **THEN** UI 提供 ADB 环境配置指引

### Requirement: 系统能够选择并连接指定设备
系统 SHALL 允许用户从设备列表中选择目标设备，并将其设置为后续所有 ADB 操作的默认设备。

#### Scenario: 选择单个设备
- **WHEN** 用户点击设备列表中的某个设备
- **THEN** 系统高亮显示该设备
- **THEN** 后续截图、UI dump、匹配测试均使用该设备

#### Scenario: 多设备环境下未选择设备
- **WHEN** 存在多个设备且用户未选择任何设备
- **THEN** 系统禁用所有依赖设备的功能按钮
- **THEN** 显示“请先选择目标设备”提示

### Requirement: 系统能够支持 USB 和 TCP/IP 连接模式
系统 SHALL 支持通过 USB 和 TCP/IP 两种方式连接 Android 设备。

#### Scenario: USB 连接模式
- **WHEN** 设备通过 USB 线连接到计算机
- **THEN** 系统通过 `AdbClient.GetDevices()` 自动检测到设备
- **THEN** 设备列表显示连接类型为 USB

#### Scenario: TCP/IP 连接模式
- **WHEN** 用户输入设备 IP 地址和端口并点击“连接”
- **THEN** 系统调用 `AdbClient.Connect(address)` 建立连接
- **THEN** 连接成功后设备列表显示连接类型为 TCP/IP

### Requirement: 系统能够实时输出操作日志
系统 SHALL 提供日志面板，实时流式输出 ADB 操作的执行结果，并记录耗时与状态。

#### Scenario: 成功执行操作并输出日志
- **WHEN** 系统执行截图拉取或 UI dump 拉取
- **THEN** 日志面板显示操作开始、完成时间与结果
- **THEN** 日志内容记录 API 调用结果而不是命令文本

#### Scenario: 操作失败输出日志
- **WHEN** ADB 操作失败或超时
- **THEN** 日志面板显示失败原因
- **THEN** UI 同时显示用户可读的错误提示

### Requirement: 系统能够异步执行 ADB 操作避免 UI 阻塞
系统 SHALL 使用 async/await 与 CancellationToken 执行所有 ADB 操作，确保 UI 线程不被阻塞。

#### Scenario: 长时间操作不阻塞 UI
- **WHEN** 系统执行耗时 ADB 操作
- **THEN** UI 保持响应
- **THEN** 操作执行期间显示加载状态
