# Development Guide

This document is for contributors working on `autojs6-dev-tools-plus`.

## Source of truth

- Project files are the source of truth for dependency direction and package selection.
- When this file conflicts with `*.csproj`, the project files win and this file must be updated immediately.
- Architecture dependency direction must remain:

```text
App.Avalonia -> Infrastructure -> Core
```

## Local build and test

```bash
dotnet restore autojs6-dev-tools-plus.sln
dotnet build autojs6-dev-tools-plus.sln
dotnet test tests/Core.Tests/Core.Tests.csproj
```

## OpenCV native dependency policy

The repository intentionally separates the managed OpenCV wrapper from native runtime provisioning.

### Managed package baseline

- Managed wrapper: `OpenCvSharp4`
- Integration boundary: `Infrastructure`
- Do **not** reference OpenCV runtime packages from `Core` or `App.Avalonia`

### Windows policy

Windows is the only platform currently wired to an in-repo OpenCV runtime package.

- Managed package: `OpenCvSharp4`
- Native runtime package: `OpenCvSharp4.runtime.win`
- The runtime package is referenced in `Infrastructure/Infrastructure.csproj`
- This is the only native runtime package currently pinned by the repository

### macOS policy

macOS native runtime provisioning is intentionally **not** pinned in `*.csproj` right now.

This is deliberate and must not be "fixed" casually.

#### Why

- The repository keeps the managed wrapper in source control
- The commonly discovered macOS OpenCvSharp runtime package is version-skewed versus the pinned managed package
- Blindly pinning an older macOS runtime package in the project is likely to create hidden ABI/version mismatches

#### Current rule

- Keep `OpenCvSharp4` in the repo
- Do **not** add `OpenCvSharp4.runtime.osx.*` to the project unless the exact version pair has been manually verified
- Provision macOS native libraries outside the repository package graph during environment setup or release packaging

#### If you need to revisit this

Before changing the macOS strategy, you must verify all of the following:

1. Managed package version and macOS native runtime version are compatible
2. `OpenCvSharpExtern` loads successfully on macOS
3. Template match smoke tests pass on a real macOS machine
4. This file, `DEVELOPMENT_zh_CN.md`, and `docs/cross-platform-build.md` are updated together

### Linux policy

Linux support is currently an architecture-reservation path.

- The repo keeps Linux `dotnet publish` entry points
- The repo does **not** currently pin a Linux OpenCV native runtime package
- Native dependency validation is handled as an explicit probe step, not as an assumed guarantee

## Linux native dependency probe plan

A probe script is provided at:

```text
scripts/linux/probe-opencv-native.sh
```

Use it after publishing on Linux:

```bash
bash scripts/linux/probe-opencv-native.sh artifacts/linux-x64
```

What it checks:

1. `OpenCvSharp.dll` exists in the publish output
2. `libOpenCvSharpExtern.so` can be found either in the publish output or common system library paths
3. `ldd` resolves the native dependency chain without `not found`

### Important

Passing the probe means the native dependency chain looks loadable.

It does **not** replace runtime smoke testing.

## Pitfalls to avoid

- Do not move OpenCV runtime references into `Core`
- Do not add a stale macOS runtime package just because NuGet search finds one
- Do not claim Linux native support is complete just because `dotnet publish` succeeds
- Do not change platform native packaging strategy without updating both development docs
