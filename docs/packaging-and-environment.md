# Packaging, Installation, and Environment Setup

This document describes how to build, publish, install, and prepare the runtime environment for `autojs6-dev-tools-plus`.

## 1. Shared prerequisites

Before running the application on any platform, make sure the following are available:

- `.NET SDK 8.0.420` for development builds
- `ADB` (`platform-tools`) available in `PATH`, or configured through `ANDROID_SDK_ROOT` / `ANDROID_HOME`
- A reachable Android device or emulator if you want to use screenshot and UI dump workflows

Recommended verification commands:

```bash
dotnet --info
adb version
adb devices
```

## 2. Local development run

```bash
dotnet restore autojs6-dev-tools-plus.sln
dotnet build autojs6-dev-tools-plus.sln
dotnet run --project App.Avalonia/App.Avalonia.csproj
```

## 3. Publish commands

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

## 4. Platform notes

### Windows

- The repository currently pins `OpenCvSharp4.runtime.win`
- Native OpenCV runtime files are expected to flow with the published output
- `adb.exe` is auto-discovered from `PATH` or common Android SDK locations

Recommended smoke checks after publish:

```powershell
Get-ChildItem artifacts/win-x64
.\artifacts\win-x64\App.Avalonia.exe
```

### macOS

- The repository intentionally **does not** pin `OpenCvSharp4.runtime.osx.*` in `*.csproj`
- macOS OpenCV native provisioning must be handled in environment preparation or release packaging
- Do not “fix” this by blindly adding an older runtime package without version verification

Before a real macOS release, verify:

1. `OpenCvSharpExtern` native library is present
2. The managed/native version pair is known-good
3. Screenshot capture, UI dump, and template match smoke tests pass on a real macOS host

### Linux

- Linux remains a reserved compatibility path
- The publish entry exists and should continue to compile
- Native OpenCV validation is handled separately from the main project graph

On a Linux host, after publishing:

```bash
bash scripts/linux/probe-opencv-native.sh artifacts/linux-x64
```

The probe checks:

- `OpenCvSharp.dll` exists
- `libOpenCvSharpExtern.so` can be found
- `ldd` does not report unresolved native dependencies

## 5. Runtime environment checklist

### Device workflow checklist

- `adb devices` shows at least one `device`
- Screen is unlocked
- UI dump is allowed on the device/emulator

### Image workflow checklist

- Screenshot can be captured or loaded from a local file
- Template image exists
- Crop region is valid if region-based search is enabled

### Widget workflow checklist

- Current screenshot matches the current UI dump source
- UI tree has been pulled successfully
- A target node is selected before generating widget-mode code

## 6. CI baseline

The repository CI currently does the following:

1. Restore
2. Build
3. Run `Core.Tests`
4. Publish `win-x64`, `osx-x64`, `linux-x64`

See `.github/workflows/build.yml`.

## 7. Release cautions

- Do not publish macOS releases without explicit native OpenCV verification
- Do not claim Linux runtime support based only on `dotnet publish`
- Treat Windows as the current strongest packaged target
