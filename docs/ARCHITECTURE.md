# Architecture

## Projects
- `FileMerger.Core` (net8.0): all pure logic, no UI types.
- `FileMerger` (net8.0-windows, WPF): views, theming, OS integration.
- `FileMerger.Tests` (xUnit): tests Core only.

## Core
| Class | Responsibility |
|---|---|
| `MergeItem` | Immutable record: path, name, cached content, on-disk size, line count. |
| `TextFileReader` | Size limit, binary detection, encoding detection, reads into `MergeItem`. |
| `FileCollector` | Expands folders recursively (skips ignored dirs), dedupes against known paths. |
| `MergeBuilder` | Builds merged text from items (single format template) + totals. |
| `LineCounter` | Line count rules used by item rows and totals. |
| `SizeFormatter` | `NNB` / `KB` / `MB` formatting. |
| `OutputNameBuilder` | Auto-name, custom-name normalization, sanitizing, truncation, collision suffix. |
| `AppSettings` / `SettingsStore` | Settings model + JSON (source-generated) load/save, fails silently. |

## WPF
| Class | Responsibility |
|---|---|
| `App` | Loads settings and theme before showing `MainWindow`; `ShutdownMode=OnMainWindowClose`. |
| `ThemeService` | Reads `AppsUseLightTheme`, swaps theme dictionary, listens to `UserPreferenceChanged`. |
| `MainViewModel` | File list, totals, debounced background preview rebuild, output name, save. |
| `MainWindow` | UI glue: drag/drop, paste, reorder, dialogs, title bar, window placement. |
| `NativeMethods` | `WM_GETMINMAXINFO` handling via monitor APIs. |
| `RelayCommand` | Minimal `ICommand`. |

## Data flow
1. Drop / paste / dialog → `MainViewModel.AddFiles(paths)`.
2. `FileCollector` expands + dedupes → `TextFileReader` reads each file (background) → `MergeItem`s appended; skip reasons → status bar.
3. Any list change → 80 ms debounce → `MergeBuilder.Build` on a thread-pool task → preview text + totals set on UI thread (stale results discarded).
4. Save → `OutputNameBuilder` resolves unique path → write merged text (UTF-8 no BOM, CRLF) → status + `Show file` enabled.
5. Settings changes → debounced `SettingsStore.Save`; also on exit.
