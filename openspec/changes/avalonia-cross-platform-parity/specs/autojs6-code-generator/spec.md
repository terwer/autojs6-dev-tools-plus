## ADDED Requirements

### Requirement: 系统能够生成图像模式 AutoJS6 代码
系统 SHALL 基于裁剪区域与模板图像，自动生成 AutoJS6 图像匹配代码，包含截图权限申请、模板加载、区域匹配、点击逻辑与必要的重试控制。

#### Scenario: 生成基础图像匹配代码
- **WHEN** 用户在图像模式下点击“生成代码”按钮
- **THEN** 系统生成包含 `requestScreenCapture()`、`images.read()`、`images.findImage()` 和 `click()` 的代码
- **THEN** 代码预览面板显示生成结果

#### Scenario: 生成带区域限制的代码
- **WHEN** 用户已创建有效裁剪区域
- **THEN** 生成的代码包含 `region: [x, y, w, h]` 参数
- **THEN** 坐标使用左上角原点规则

### Requirement: 系统能够生成控件模式 AutoJS6 代码
系统 SHALL 基于选中控件节点生成最优 UiSelector 选择器链，优先 `id()`，降级 `text()` 与 `desc()`，必要时追加 `boundsInside()`。

#### Scenario: 生成基于 resource-id 的选择器
- **WHEN** 选中控件包含 resource-id
- **THEN** 系统生成以 `id()` 为核心的选择器链
- **THEN** 生成结果可直接用于 `.findOne()`

#### Scenario: 生成降级选择器
- **WHEN** 选中控件缺少 resource-id 但包含 text 或 content-desc
- **THEN** 系统生成 `text()` 或 `desc()` 选择器
- **THEN** 保持控件模式与图像模式严格独立

### Requirement: 系统能够遵守 Rhino 与 AutoJS6 运行约束
系统 SHALL 在生成代码时强制遵守 Rhino 与 AutoJS6 约束，包括循环体内使用 `var`、单轮单截图、及时回收图像对象、region 优先。

#### Scenario: 循环体变量约束
- **WHEN** 系统生成带重试逻辑的代码
- **THEN** 循环体内部使用 `var`
- **THEN** 不在循环体内生成 `const` 或 `let`

#### Scenario: 图像回收约束
- **WHEN** 系统生成图像模式代码
- **THEN** 模板图像和临时图像对象具备明确回收路径
- **THEN** 代码遵守 OOM 预防规则

### Requirement: 系统能够处理路径兼容性与命名规范
系统 SHALL 自动切换 assets、相对路径与绝对路径，并统一输出正斜杠路径格式。

#### Scenario: 生成 assets 路径
- **WHEN** 模板位于项目 assets 目录
- **THEN** 系统生成 `./assets/...` 风格路径
- **THEN** 路径分隔符统一为正斜杠

#### Scenario: 自定义变量名前缀
- **WHEN** 用户指定变量前缀或变量名
- **THEN** 系统在代码中使用用户指定名称
- **THEN** 保持命名一致且可读

### Requirement: 系统能够格式化、预览、编辑并导出 JS 代码
系统 SHALL 提供代码预览面板，支持格式化、手动编辑、复制与导出 `.js` 文件。

#### Scenario: 代码自动格式化
- **WHEN** 系统生成代码
- **THEN** 使用 2 空格缩进与清晰注释进行格式化
- **THEN** 关键步骤具备必要说明注释

#### Scenario: 一键复制代码
- **WHEN** 用户点击“复制代码”按钮
- **THEN** 系统将完整代码写入剪贴板
- **THEN** UI 显示复制成功提示

#### Scenario: 导出为 JS 文件
- **WHEN** 用户点击“导出文件”按钮
- **THEN** 系统打开文件保存对话框并导出 `.js` 文件
- **THEN** 默认文件名可读且可追踪
