# AGENTS.md

FileMerger: portable WPF (.NET 8) tool that merges many text files into one text file with a live preview.

## Layout
- `src/FileMerger.Core` – pure logic (net8.0, no WPF), unit-tested.
- `src/FileMerger` – WPF app (net8.0-windows), UI glue only.
- `tests/FileMerger.Tests` – xUnit tests for Core.
- `resources/icon.ico` – provided by the owner. Never generate or overwrite.
- `docs/` – PROJECT (spec), STYLE (design/palettes), ARCHITECTURE, CONVENTIONS, TASKS, DECISIONS.

## Commands
- Build: `dotnet build FileMerger.sln -c Release`
- Test: `dotnet test tests/FileMerger.Tests -c Release`
- Publish (framework-dependent): `dotnet publish src/FileMerger -c Release -p:PublishProfile=FrameworkDependent`
- Publish (self-contained): `dotnet publish src/FileMerger -c Release -p:PublishProfile=SelfContained`
- On Linux WPF only compiles (`EnableWindowsTargeting`); CI on `windows-latest` is the final judge.

## Operating rules
- Work autonomously; never wait for approval. Ambiguity → pick the simplest option, log one line in `docs/DECISIONS.md`.
- Work on `main`; small logical commits, short imperative messages; push after each phase.
- Token economy: scaffold with `dotnet new`, grep instead of reading whole files, batch edits, build/test per phase not per edit, minimal prose.
- Docs stay concise: AGENTS.md ≤ 40 lines, other docs ≤ 120 lines, no duplication between docs.
- C# formatting: never wrap method call arguments or signatures across lines.
- No WinForms. Dialogs: `Microsoft.Win32.OpenFileDialog` / `OpenFolderDialog`.
- Keep all testable logic in Core.
