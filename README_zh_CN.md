# AutoJS6 可视化开发工具包增强版

> **⚠️ 项目开发中** - 当前仍处于早期开发阶段，功能仍在朝完整产品形态持续演进，请勿用于生产环境。

[English](README.md) | [简体中文](README_zh_CN.md)

🎯 面向 AutoJS6 脚本开发者的跨平台开发工具包，提供可视化截图分析、UI 控件解析、图像匹配预览和 AutoJS6 脚本代码生成。

> **选择与你的平台目标一致的版本**  
> `autojs6-dev-tools-plus` 面向 Windows 与 macOS 的跨平台工作流。  
> 如果你的重点是 Windows-only、性能优先的使用体验，请选择 `autojs6-dev-tools`。  
> 两个项目都由我原创并持续维护，分别服务于同一工具体系下不同的产品目标。

> **图像识别调试 20 次才跑通？换台设备又失效？**  
> 用这个工具：实时预览匹配结果 • 可视化调整阈值和区域 • 自动生成 AutoJS6 代码 • 首发支持 Windows 和 macOS

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-12.0.1-7C3AED)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux--ready-2ea44f)](#-快速开始)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## 😫 你肯定经历过的痛苦

**没有这个工具开发 AutoJS6 脚本时：**

1. 📸 截图 → 手动裁剪模板 → 保存 → 写代码 → 跑真机测试
2. 📝 猜坐标（x: 500？520？540？）→ 写代码 → 跑真机测试
3. ❌ 模板没找到 → 调整裁剪 2 像素 → 再跑一次
4. 🔄 重复 20 次直到能用
5. 📱 换台设备测试 → 分辨率不同 → 从头再来
6. 🤔 阈值设 0.8 还是 0.85？→ 一个个在真机上试
7. 🌲 需要 resource-id？→ 在 5000 个 UI 节点里手动翻
8. 💥 点击偏了 10 像素 → 重新算偏移量 → 再跑一次

**每天浪费几小时。天天如此。**

---

## ✨ 这个工具实际能做什么

**在真机运行之前就能看到模板匹配结果：**
- 拖拽裁剪模板 → 立即看到匹配置信度（0.95？0.62？）
- 调节阈值滑块 → 实时看到匹配结果出现/消失
- 裁剪不对？调整 2 像素 → 立即看到结果
- 不再需要“跑 → 失败 → 调整 → 再跑”的循环

**用鼠标拾取坐标，不用猜：**
- 鼠标悬停截图 → 看到精确像素坐标（x: 523, y: 187）
- 点击标记 → 坐标自动复制到剪贴板
- 拖拽矩形 → 自动获取区域 `[x, y, w, h]`
- 不再需要“试试 x+10... 不对，试试 x+15...”

**自动生成 AutoJS6 代码：**
- 选择模板 → 点击“生成代码” → 得到完整脚本
- 图像模式：`images.findImage()` 带正确的阈值和区域
- 控件模式：`id().text().findOne()` 带降级选择器
- 复制粘贴即用，不用手写

**无需真机也能更高效地测试多分辨率：**
- 加载 3 台设备的截图 → 在所有截图上测试模板
- 看到哪个分辨率失败 → 调整一次裁剪 → 全部通过
- 不再“我手机能用但用户手机不行”

---

## 💡 谁需要这个工具？

**你需要这个工具，如果：**
- ✅ 每天花 >30 分钟裁剪截图和调整坐标
- ✅ 在多台不同分辨率的 Android 设备上测试脚本
- ✅ 频繁使用图像识别功能
- ✅ 需要手动搜索 UI 树找控件属性
- ✅ 想在不跑真机的情况下预览匹配结果

**你不需要这个工具，如果：**
- ❌ 只用简单的固定坐标点击
- ❌ 从不使用图像匹配或控件选择器
- ❌ 享受每个功能手动调试 20 次的乐趣

---

## 🚀 快速开始

### 前置要求

- **💻 操作系统**：Windows / macOS
- **⚙️ 运行时**：.NET 8 SDK
- **🛠️ IDE**：Rider、Visual Studio 或带 C# 支持的 VS Code
- **📱 工具**：Android Debug Bridge (ADB) 在 PATH 中

### 构建并运行

```bash
# 恢复 NuGet 包
dotnet restore App.Avalonia/App.Avalonia.csproj

# 构建项目
dotnet build App.Avalonia/App.Avalonia.csproj

# 运行应用
dotnet run --project App.Avalonia/App.Avalonia.csproj
```

也可以直接在 IDE 中打开 `autojs6-dev-tools-plus.slnx`，运行 `App.Avalonia` 项目。

---

## 🖼️ 项目截图

截图资源放在 `docs/images/` 目录。

预留图片槽位：
- `docs/images/main-workspace.png`
- `docs/images/screenshot-canvas.png`
- `docs/images/ui-tree-inspector.png`
- `docs/images/template-match-preview.png`
- `docs/images/code-generator.png`

---

## ✨ 核心功能

### 🖼️ 图像处理引擎（像素级）

- **📸 实时截图捕获**：一键通过 ADB 拉取设备截图
- **✂️ 交互式裁剪**：拖拽顶点/边调整，Shift 锁定宽高比
- **🎯 像素坐标拾取**：鼠标悬停显示精确坐标，Ctrl 十字准线锁定
- **🔍 OpenCV 模板匹配**：`TM_CCOEFF_NORMED` 算法，可调阈值（0.50-0.95）
- **💾 模板导出**：保存裁剪区域为 PNG，附带偏移量元数据

### 🌲 UI 图层分析引擎（控件级）

- **📱 Android UI 树解析**：拉取并解析 `uiautomator` dump 数据
- **🧹 智能布局过滤**：自动移除冗余布局容器
- **🎨 控件边界框渲染**：按类型着色（蓝色=文本，绿色=按钮，橙色=图片）
- **🔗 双向同步**：点击 TreeView → 高亮画布，点击画布 → 展开 TreeView
- **📋 属性面板**：一键复制坐标、文本或 XPath 表达式

### 🎨 跨平台交互式画布

- **⚡ 分层渲染**：跨平台截图层 + overlay 层双图层工作流
- **🔍 缩放与平移**：鼠标滚轮缩放（10%-500%，以光标为中心），拖拽平移带惯性
- **🔄 旋转支持**：90° 步进旋转，坐标系保持一致
- **📏 辅助工具**：像素标尺、10x10 网格、十字准线锁定

### 🤖 AutoJS6 代码生成器

**图像模式**（基于像素匹配）
```javascript
// 自动生成的 AutoJS6 代码
requestScreenCapture();
var template = images.read("./assets/login_button.png");
var result = images.findImage(screen, template, {
    threshold: 0.85,
    region: [100, 200, 300, 400]
});
if (result) {
    click(result.x + 150, result.y + 25);
    log("点击登录按钮");
}
template.recycle();
```

**控件模式**（基于选择器）
```javascript
// 自动生成的 AutoJS6 代码
var widget = id("com.example:id/login_button").findOne();
if (!widget) widget = text("登录").findOne();
if (!widget) widget = desc("登录按钮").findOne();
if (widget) {
    widget.click();
    log("点击登录按钮");
}
```

### ⚡ 实时匹配测试

- **🎚️ 实时阈值调节**：滑块（0.50-0.95）带即时视觉反馈
- **✅ UiSelector 验证**：对当前 UI 树测试选择器
- **📐 坐标对齐检查**：验证控件边界与截图像素是否匹配
- **📊 批量测试**：加载多个模板，生成汇总报告

---

## 📁 项目结构

```text
autojs6-dev-tools-plus/
├── App.Avalonia/              # Avalonia UI 层
│   ├── Views/                 # 页面与自定义控件
│   ├── ViewModels/            # MVVM 视图模型
│   └── Resources/             # 样式与资源字典
├── Core/                      # 纯业务逻辑（无 UI 依赖）
│   ├── Abstractions/          # 服务接口
│   ├── Models/                # 数据模型
│   ├── Services/              # 核心业务服务
│   └── Helpers/               # 工具类
├── Infrastructure/            # 基础设施层
│   ├── Adb/                   # ADB 通信
│   ├── Imaging/               # 图像处理封装
│   └── Platform/              # 剪贴板、对话框、文件服务等
├── tests/                     # 单元测试
├── docs/images/               # README 截图
├── openspec/                  # OpenSpec 变更提案
├── AGENTS.md                  # 核心设计原则（AI agent 上下文）
├── README.md                  # 英文 README
└── README_zh_CN.md            # 中文 README
```

---

## 🏗️ 架构原则

### 🔀 双引擎独立（严格隔离）

- **🖼️ 图像引擎**：像素/位图 → 绝对像素坐标 `(x, y, w, h)`
- **🌲 UI 引擎**：控件树 → UiSelector 链（`id().text().findOne()`）
- **🚫 零耦合**：数据源、处理管线、渲染逻辑、代码生成路径完全解耦

### ⬇️ 单向依赖

```text
App.Avalonia → Infrastructure → Core
```

- **🎯 Core**：纯业务逻辑，无 UI 依赖，可独立测试
- **🔌 Infrastructure**：外部依赖封装（ADB、图像处理、平台服务）
- **🎨 App.Avalonia**：仅 UI 与 MVVM

### ⚡ 异步优先架构

- 所有 I/O 操作（ADB、OpenCV、XML 解析、纹理上传）使用 `async/await`
- UI 线程永不阻塞
- 后台操作支持 `CancellationToken`

---

## 🛠️ 关键技术

| 组件 | 技术 | 用途 |
|-----------|-----------|---------|
| 🎨 UI 框架 | Avalonia | 跨平台桌面 UI |
| 🖼️ 渲染 | 分层交互式画布 | 跨平台截图与 overlay 渲染 |
| 🔍 图像处理 | OpenCV + SixLabors.ImageSharp | 模板匹配与图像处理 |
| 📱 ADB 通信 | AdvancedSharpAdbClient | Android 设备控制 |
| 🔗 MVVM | CommunityToolkit.Mvvm | 视图模型绑定与命令 |
| 🏗️ 架构 | Clean Architecture | 分层关注点分离 |

---

## ⚠️ AutoJS6 代码生成约束

生成的代码必须符合 AutoJS6 运行时约束：

### 🐛 Rhino 引擎限制

```javascript
// ❌ 错误：循环体内用 const/let（Rhino bug - 变量不会重新绑定）
while (true) {
    const result = computeSomething();
    process(result);  // result 保持第一次迭代的值！
}

// ✅ 正确：循环体内用 var
while (true) {
    var result = computeSomething();
    process(result);  // result 每次迭代正确更新
}
```

### 💾 OOM 预防

- **📸 单轮单截图**：永远不要在一个循环里多次调用 `captureScreen()`
- **🎯 最小化场景检测范围**：不要每次迭代扫描所有模板
- **📐 优先基于区域匹配**：使用 `region: [x, y, w, h]` 而不是全屏
- **♻️ 回收 ImageWrapper 对象**：使用后立即调用 `.recycle()`

### ✂️ 模板裁剪规则

**✅ 包含**：文字、图标、固定边框  
**❌ 排除**：红点、数字、倒计时、动态数值

---

## 🤝 贡献

欢迎贡献！请：

1. 🍴 Fork 仓库
2. 🌿 创建功能分支（`git checkout -b feature/amazing-feature`）
3. 📖 仔细阅读 `AGENTS.md` 和 `openspec/` 文档
4. 🏗️ 遵循架构原则
5. ✅ 为 Core 层变更编写测试
6. 💬 清晰的提交信息（`git commit -m 'add amazing feature'`）
7. 🚀 推送到你的分支（`git push origin feature/amazing-feature`）
8. 🔀 打开 Pull Request

---

## 📚 文档

- **📘 AGENTS.md**：核心设计原则与约束
- **📗 openspec/**：产品提案、设计决策与实施任务

---

## 📄 许可证

本项目采用 MIT 许可证，详见 [LICENSE](LICENSE) 文件。
