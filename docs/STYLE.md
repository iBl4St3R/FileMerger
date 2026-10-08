# Style

Catppuccin look, matching the "Apex" app. Flat, compact, built for a narrow window docked next to Explorer.

## Window
- Borderless, `WindowChrome`: CaptionHeight 44, ResizeBorderThickness 4, GlassFrameThickness 0.
- Default ~480×760, MinWidth 380, MinHeight 520. Maximize respects the work area (`WM_GETMINMAXINFO`).
- Layout top→bottom: title bar (44) → drop zone (~90) → file list (~25%, `GridSplitter`) → preview (rest) → output panel → action row → status bar.

## Elements
- Font Segoe UI 13; preview Consolas 12.
- Corners 6–8 px, 1 px borders, hand cursor on buttons, hover states everywhere.
- Title bar: icon + title left; pin / minimize / maximize / close right (36×36, radius 6, hover fill; close hover `#E24B4A`).
- Drop zone: dashed rounded border over a dotted grid (tiled `DrawingBrush`); accent border on drag-over.
- Scrollbars: thin, rounded, themed.
- Save: large green primary; Show file: large secondary.

## Theming
- `Themes/Dark.xaml` and `Themes/Light.xaml`, identical keys, consumed via `DynamicResource`.
- Follows Windows app mode (`AppsUseLightTheme`: 0 dark, 1/missing light), applied before the window shows, live via `SystemEvents.UserPreferenceChanged`. No manual setting.

| Key | Token | Dark (Mocha) | Light (Latte) |
|---|---|---|---|
| WindowBg | Window bg | #1E1E2E | #EFF1F5 |
| PanelBg | Panel bg | #181825 | #E6E9EF |
| BarBg | Title/status bar | #11111B | #DCE0E8 |
| Surface | Buttons, inputs | #313244 | #CCD0DA |
| SurfaceHover | Hover / border | #45475A | #BCC0CC |
| Text | Text | #CDD6F4 | #4C4F69 |
| Muted | Muted text | #6C7086 | #6C6F85 |
| Accent | Drag-over, focus, toggle | #CBA6F7 | #8839EF |
| Info | Link/info | #89B4FA | #1E66F5 |
| Save | Save button (white text) | #1D9E75 | #40A02B |
| Danger | Danger | #E24B4A | #D20F39 |

Brush resource keys are the "Key" column with a `Brush` suffix (e.g. `WindowBgBrush`).
