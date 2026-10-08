# Conventions

## C#
- `Nullable` and `ImplicitUsings` enabled (set in `Directory.Build.props`); file-scoped namespaces.
- PascalCase types/members, `_camelCase` private fields, `camelCase` locals/parameters.
- One public type per file; file name = type name.
- `sealed` by default; `static` for stateless helpers.
- Never wrap method call arguments or signatures across lines, however long.
- Prefer simple code over abstractions; no MVVM framework, no DI container.

## Layering
- Pure logic lives in Core and is unit-tested. No WPF/Win32 types in Core.
- Code-behind contains only UI glue (event wiring, dialogs, drag visuals, window chrome).
- XAML uses `DynamicResource` for every themed brush.

## Errors
- User-facing problems (skipped files, IO failures on save) → short status-bar message, never a modal dialog.
- Settings IO never throws: corrupt/unwritable → defaults / ignore.
- Catch specific IO exceptions (`IOException`, `UnauthorizedAccessException`); do not swallow programming errors elsewhere.

## Build
- Aim for 0 warnings. Target framework is defined once in `Directory.Build.props`.
- Tests: xUnit, one test class per Core class, temp files under `Path.GetTempPath()` cleaned up.
