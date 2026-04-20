# AutoJS6 Visual Development Toolkit Plus

> **⚠️ Under Active Development** - This project is in early development stage. Features are still evolving toward full product parity. Do not use in production.

[English](README.md) | [简体中文](README_zh_CN.md)

🎯 A cross-platform development toolkit for AutoJS6 script developers, with visual screenshot analysis, UI widget parsing, image matching preview, and AutoJS6 script code generation.

> **Choose the edition that fits your platform strategy**  
> `autojs6-dev-tools-plus` is the Cross-platform edition for Windows and macOS.  
> For the Windows-native edition, see [`autojs6-dev-tools`](https://github.com/terwer/autojs6-dev-tools).  
> Both projects are original works that I design and maintain as part of the same AutoJS6 toolkit family.

> **Image recognition takes 20 tries to work? Breaks on different devices?**  
> Use this tool: Real-time match preview • Visual threshold and region adjustment • Auto-generate AutoJS6 code • Windows and macOS first

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-12.0.1-7C3AED)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux--ready-2ea44f)](#-quick-start)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## 😫 The Pain You Know Too Well

**Developing AutoJS6 scripts without this tool:**

1. 📸 Screenshot → Manually crop template → Save → Write code → Run on device
2. 📝 Guess coordinates (x: 500? 520? 540?) → Write code → Run on device
3. ❌ Template not found → Adjust crop by 2 pixels → Run again
4. 🔄 Repeat 20 times until it works
5. 📱 Test on another device → Different resolution → Start over
6. 🤔 Threshold 0.8 or 0.85? → Try one by one on real device
7. 🌲 Need resource-id? → Manually search through 5000 UI nodes
8. 💥 Click missed by 10 pixels → Recalculate offset → Run again

**Hours wasted. Every. Single. Day.**

---

## ✨ What This Tool Actually Does

**See template matching results BEFORE running on device:**
- Drag to crop template → Instantly see match confidence (0.95? 0.62?)
- Adjust threshold slider → Watch matches appear/disappear in real-time
- Wrong crop? Adjust 2 pixels → See result immediately
- No more "run → fail → adjust → run" loops

**Pick coordinates with mouse, not guesswork:**
- Hover over screenshot → See exact pixel coordinates (x: 523, y: 187)
- Click to mark → Coordinates copied to clipboard
- Drag rectangle → Get region `[x, y, w, h]` automatically
- No more "let me try x+10... no wait, x+15..."

**Generate AutoJS6 code automatically:**
- Select template → Click "Generate Code" → Get complete script
- Image mode: `images.findImage()` with correct threshold and region
- Widget mode: `id().text().findOne()` with fallback selectors
- Copy-paste ready, no manual typing

**Test on multiple resolutions without real devices:**
- Load screenshots from 3 devices → Test template on all
- See which resolution fails → Adjust crop once → Works everywhere
- No more "works on my phone but not on user's phone"

---

## 💡 Who Needs This?

**You need this if you:**
- ✅ Spend >30 minutes per day cropping screenshots and adjusting coordinates
- ✅ Test scripts on multiple Android devices with different resolutions
- ✅ Use image recognition features frequently
- ✅ Need to manually search UI tree for widget attributes
- ✅ Want to preview matching results without running on device

**You DON'T need this if:**
- ❌ You only use simple fixed-coordinate clicks
- ❌ You never use image matching or widget selectors
- ❌ You enjoy manually debugging 20 times per feature

---

## 🚀 Quick Start

### Prerequisites

- **💻 OS**: Windows / macOS
- **⚙️ Runtime**: .NET 8 SDK
- **🛠️ IDE**: Rider, Visual Studio, or VS Code with C# support
- **📱 Tools**: Android Debug Bridge (ADB) in PATH

### Build and Run

```bash
# Restore NuGet packages
dotnet restore App.Avalonia/App.Avalonia.csproj

# Build project
dotnet build App.Avalonia/App.Avalonia.csproj

# Run application
dotnet run --project App.Avalonia/App.Avalonia.csproj
```

Or open `autojs6-dev-tools-plus.slnx` in your IDE and run the `App.Avalonia` project.

---

## 🖼️ Screenshots

Screenshots will be placed under `docs/images/`.

Reserved image slots:
- `docs/images/main-workspace.png`
- `docs/images/screenshot-canvas.png`
- `docs/images/ui-tree-inspector.png`
- `docs/images/template-match-preview.png`
- `docs/images/code-generator.png`

---

## ✨ Features

### 🖼️ Image Processing Engine (Pixel-Level)

- **📸 Real-Time Screenshot Capture**: Pull device screenshots via ADB with one click
- **✂️ Interactive Cropping**: Drag vertices/edges to adjust, Shift to lock aspect ratio
- **🎯 Pixel Coordinate Picker**: Mouse hover shows exact coordinates, Ctrl for crosshair lock
- **🔍 OpenCV Template Matching**: `TM_CCOEFF_NORMED` algorithm with adjustable threshold (0.50-0.95)
- **💾 Template Export**: Save cropped regions as PNG with offset metadata

### 🌲 UI Layer Analysis Engine (Widget-Level)

- **📱 Android UI Tree Parsing**: Pull and parse `uiautomator` dump data
- **🧹 Smart Layout Filtering**: Automatically remove redundant layout containers
- **🎨 Widget Boundary Rendering**: Color-coded by type (Blue=Text, Green=Button, Orange=Image)
- **🔗 Bidirectional Sync**: Click TreeView → highlight canvas, click canvas → expand TreeView
- **📋 Property Panel**: One-click copy coordinates, text, or XPath expressions

### 🎨 Cross-Platform Interactive Canvas

- **⚡ Layered Rendering**: Cross-platform screenshot layer + overlay layer workflow
- **🔍 Zoom & Pan**: Mouse wheel zoom (10%-500%, cursor-centered), drag to pan with inertia
- **🔄 Rotation Support**: 90° step rotation with coordinate system preservation
- **📏 Auxiliary Tools**: Pixel ruler, 10x10 grid, crosshair lock

### 🤖 AutoJS6 Code Generator

**Image Mode** (Pixel-based matching)
```javascript
// Auto-generated AutoJS6 code
requestScreenCapture();
var template = images.read("./assets/login_button.png");
var result = images.findImage(screen, template, {
    threshold: 0.85,
    region: [100, 200, 300, 400]
});
if (result) {
    click(result.x + 150, result.y + 25);
    log("Clicked login button");
}
template.recycle();
```

**Widget Mode** (Selector-based)
```javascript
// Auto-generated AutoJS6 code
var widget = id("com.example:id/login_button").findOne();
if (!widget) widget = text("Login").findOne();
if (!widget) widget = desc("Login Button").findOne();
if (widget) {
    widget.click();
    log("Clicked login button");
}
```

### ⚡ Real-Time Match Testing

- **🎚️ Live Threshold Adjustment**: Slider (0.50-0.95) with instant visual feedback
- **✅ UiSelector Validation**: Test selectors against current UI tree
- **📐 Coordinate Alignment Check**: Verify widget bounds match screenshot pixels
- **📊 Batch Testing**: Load multiple templates, generate summary report

---

## 📁 Project Structure

```text
autojs6-dev-tools-plus/
├── App.Avalonia/              # Avalonia UI layer
│   ├── Views/                 # Pages and custom controls
│   ├── ViewModels/            # MVVM view models
│   └── Resources/             # Styles and resource dictionaries
├── Core/                      # Pure business logic (no UI dependencies)
│   ├── Abstractions/          # Service interfaces
│   ├── Models/                # Data models
│   ├── Services/              # Core business services
│   └── Helpers/               # Utility classes
├── Infrastructure/            # Infrastructure layer
│   ├── Adb/                   # ADB communication
│   ├── Imaging/               # Image processing wrappers
│   └── Platform/              # Clipboard, dialogs, file services, etc.
├── tests/                     # Unit tests
├── docs/images/               # README screenshots
├── openspec/                  # OpenSpec change proposals
├── AGENTS.md                  # Core design principles (AI agent context)
├── README.md                  # English README
└── README_zh_CN.md            # Chinese README
```

---

## 🏗️ Architecture Principles

### 🔀 Dual-Engine Independence (Strict Isolation)

- **🖼️ Image Engine**: Pixel/bitmap → absolute pixel coordinates `(x, y, w, h)`
- **🌲 UI Engine**: Widget tree → UiSelector chains (`id().text().findOne()`)
- **🚫 Zero Coupling**: Data sources, processing pipelines, rendering logic, and code generation paths are completely decoupled

### ⬇️ Unidirectional Dependency

```text
App.Avalonia → Infrastructure → Core
```

- **🎯 Core**: Pure business logic, no UI dependencies, independently testable
- **🔌 Infrastructure**: External dependency wrappers (ADB, image processing, platform services)
- **🎨 App.Avalonia**: UI and MVVM only

### ⚡ Async-First Architecture

- All I/O operations (ADB, OpenCV, XML parsing, texture upload) use `async/await`
- UI thread never blocked
- Background operations with `CancellationToken` support

---

## 🛠️ Key Technologies

| Component | Technology | Purpose |
|-----------|-----------|---------|
| 🎨 UI Framework | Avalonia | Cross-platform desktop UI |
| 🖼️ Rendering | Layered interactive canvas | Cross-platform screenshot and overlay rendering |
| 🔍 Image Processing | OpenCV + SixLabors.ImageSharp | Template matching and image manipulation |
| 📱 ADB Communication | AdvancedSharpAdbClient | Android device control |
| 🔗 MVVM | CommunityToolkit.Mvvm | View model binding and commands |
| 🏗️ Architecture | Clean Architecture | Layered separation of concerns |

---

## ⚠️ AutoJS6 Code Generation Constraints

Generated code must comply with AutoJS6 runtime constraints:

### 🐛 Rhino Engine Limitations

```javascript
// ❌ WRONG: const/let in loop body (Rhino bug - variable won't rebind)
while (true) {
    const result = computeSomething();
    process(result);  // result keeps first iteration value!
}

// ✅ CORRECT: Use var in loop body
while (true) {
    var result = computeSomething();
    process(result);  // result correctly updates each iteration
}
```

### 💾 OOM Prevention

- **📸 Single screenshot per iteration**: Never call `captureScreen()` multiple times in one loop
- **🎯 Minimize scene detection scope**: Don't scan all templates every iteration
- **📐 Prefer region-based matching**: Use `region: [x, y, w, h]` instead of full-screen
- **♻️ Recycle ImageWrapper objects**: Call `.recycle()` immediately after use

### ✂️ Template Cropping Rules

**✅ Include**: Text, icons, fixed borders  
**❌ Exclude**: Red dots, numbers, countdowns, dynamic values

---

## 🤝 Contributing

We welcome contributions! Please:

1. 🍴 Fork the repository
2. 🌿 Create a feature branch (`git checkout -b feature/amazing-feature`)
3. 📖 Read `AGENTS.md` and `openspec/` documents carefully
4. 🏗️ Follow the architecture principles
5. ✅ Write tests for Core layer changes
6. 💬 Commit with clear messages (`git commit -m 'add amazing feature'`)
7. 🚀 Push to your branch (`git push origin feature/amazing-feature`)
8. 🔀 Open a Pull Request

---

## 📚 Documentation

- **📘 AGENTS.md**: Core design principles and constraints
- **📗 openspec/**: Product proposals, design decisions, and implementation tasks

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
