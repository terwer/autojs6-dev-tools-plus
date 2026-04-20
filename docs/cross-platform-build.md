# Cross-platform build baseline

## Source of truth

This repository uses code as the source of truth. When documentation conflicts with project files, project files win and the docs must be updated to match.

## Current desktop target matrix

| Platform | Runtime identifier | CI action | Status |
| --- | --- | --- | --- |
| Windows | `win-x64` | Restore / Build / Test / Publish | Primary delivery target |
| macOS | `osx-x64` | Restore / Build / Test / Publish | Primary delivery target |
| Linux | `linux-x64` | Restore / Build / Test / Publish entry retained | Architecture reservation path |

## SDK baseline

- `.NET SDK`: `8.0.420`
- Pinned via `global.json`

## OpenCV baseline

- Managed wrapper: `OpenCvSharp4`
- Integration boundary: `Infrastructure`
- Native runtime packaging is intentionally separated from the Core and App layers so the dependency direction remains `App -> Infrastructure -> Core`.
- Windows currently pins `OpenCvSharp4.runtime.win` in `Infrastructure`.
- macOS native runtime is intentionally **not** pinned in `*.csproj`; it must be provisioned outside the repo package graph until a verified version pair exists.
- Linux keeps a reserved validation path via `scripts/linux/probe-opencv-native.sh`.

## GitHub Actions workflow

The repository ships a single CI workflow at `.github/workflows/build.yml`:

1. Restores the solution
2. Builds the full solution in `Release`
3. Runs `Core.Tests`
4. Publishes `App.Avalonia` for `win-x64`, `osx-x64`, and `linux-x64`

Linux currently keeps the build and publish entry so later native dependency validation can be added without changing the project graph.
