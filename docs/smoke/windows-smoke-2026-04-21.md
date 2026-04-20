# Windows Smoke Test - 2026-04-21

## Scope

Validated the following end-to-end path on a real Windows host:

1. ADB device discovery
2. Screenshot capture from device
3. UI dump pull
4. UI dump parsing
5. Template export from captured screenshot
6. OpenCV template match (`TM_CCOEFF_NORMED`)
7. AutoJS6 image-mode code generation
8. AutoJS6 widget-mode code generation

## Environment

- Date: 2026-04-21
- Host OS: Windows
- ADB path: `D:\Android\Sdk\platform-tools\adb.exe`
- Device used: `emulator-5554`
- Model: `23116PN5BC`
- Connection: `usb`

## Result Summary

- Device scan: success
- Screenshot capture: success
- UI dump: success
- Parsed business nodes: `34`
- Template export: success
- Match result: success
- Image-mode code validation: success
- Widget-mode code validation: success

## Key Evidence

- Screenshot size: `720x1280`
- Template crop: `[0, 61, 79, 79]`
- Match location: `(0, 61)`
- Match click point: `(39, 100)`
- Match confidence: `1.0000`
- Match elapsed: `2 ms`
- Algorithm: `TM_CCOEFF_NORMED`

## Artifact Paths

Original local artifacts were generated under:

```text
C:\Users\Administrator\Desktop\autojs6-smoke-output\
```

Repository snapshot copy:

- `docs/smoke/windows-smoke-2026-04-21.json`

Generated local evidence files:

- `windows-smoke-screenshot.png`
- `windows-smoke-ui.xml`
- `windows-smoke-template.png`
- `windows-smoke-template.json`
- `windows-smoke-image.js`
- `windows-smoke-widget.js`
- `windows-smoke-report.json`

## Notes

- During smoke execution, a real framebuffer handling issue was found and fixed in `Infrastructure/Adb/AdbService.cs`
- The issue was caused by `Framebuffer.Data.Length` being larger than `Framebuffer.Header.Size`
- The implementation now truncates to `Header.Size` before raw pixel conversion when needed
