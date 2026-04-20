# Capability Parity Baseline

This document defines the capability parity baseline between:

- `autojs6-dev-tools` (Windows / WinUI reference)
- `autojs6-dev-tools-plus` (Avalonia cross-platform implementation)

It is intended as an acceptance checklist rather than a marketing document.

## Parity matrix

| Capability | Windows reference baseline | Plus acceptance baseline | Status in Plus |
| --- | --- | --- | --- |
| Device discovery | Scan ADB devices and select target device | Same workflow available in Avalonia shell | Implemented |
| Screenshot capture | Capture current device screenshot | Capture screenshot or load local image | Implemented |
| UI dump parsing | Pull and parse current UI tree | Same workflow, same selector/bounds rules | Implemented |
| Image crop workflow | Create / adjust crop region, export template | Same crop-region workflow with editable handles | Implemented |
| Canvas interaction | Zoom / pan / rotate / coordinate feedback | Same capability family in Avalonia custom canvas | Implemented |
| Widget overlay rendering | Draw widget bounds over screenshot | Same overlay model with type-based colors and filters | Implemented |
| Tree ↔ canvas sync | Tree selection and canvas reverse locate | Same bidirectional linkage | Implemented |
| Image matching | Run OpenCV TM_CCOEFF_NORMED and visualize result | Same algorithm, threshold, overlay, report flow | Implemented |
| Batch template tests | Run multiple template matches and summarize | Same batch-match workflow with interruption support | Implemented |
| Selector validation | Validate generated selector against current UI tree | Highlight matches and produce validation report | Implemented |
| Coordinate alignment check | Compare dump bounds against screenshot coordinates | Output alignment delta / warning | Implemented |
| Code preview and export | Preview generated code and export `.js` | Editable preview, format, copy, export | Implemented |
| Native packaging | Windows-first packaging baseline | Windows packaged path complete; macOS/Linux have guarded release rules | Partial |
| Windows smoke test | End-to-end run on real Windows host | Still requires explicit smoke execution record | Pending |
| macOS smoke test | n/a in old project | Requires explicit smoke execution record on macOS | Pending |
| Linux runtime validation | n/a in old project | Reserved path via publish + native probe | Partial |

## Acceptance notes

### Implemented

“Implemented” means the feature has been wired into the Avalonia shell and passes repository build/test validation.

### Partial

“Partial” means architecture, publish entry, or guarded process exists, but platform-specific runtime validation is still required.

### Pending

“Pending” means the acceptance item depends on a real target host, real device workflow, or a release-stage verification pass that has not yet been recorded.

## What this table is for

Use this file as the baseline for:

- implementation review
- acceptance review
- future regression checks
- release readiness discussion
