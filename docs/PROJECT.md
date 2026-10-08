# FileMerger – functional spec

Merge many text files into ONE text file, with live preview.

## Adding files
All sources go through one `AddFiles(IEnumerable<string>)` path:
- Drag & drop from Explorer onto the drop zone.
- Ctrl+V anywhere pastes files copied in Explorer (`Clipboard.GetFileDropList()`).
- Click on the drop zone → multi-select `OpenFileDialog`.
- Folders are expanded recursively, skipping `.git`, `bin`, `obj`, `node_modules`, `.vs`.
- Duplicates (same full path) ignored; binary files and files > 25 MB skipped.
- Skips produce a short status-bar message, never a modal dialog.

## Text support
- No extension whitelist; anything readable as text.
- Binary: NUL byte in first 8 KB (unless UTF-16 BOM) → skip.
- Encoding: BOM → strict UTF-8 → system ANSI code page (`CodePagesEncodingProvider`).
- Content is read once on add and cached.

## File list
- Compact rows: file name, muted `[245 lines, 2KB]`, small ✕ remove button.
- Drag rows to reorder (order = merge order). Delete key removes selection.
- Header: `FILES · N` + `Clear` button.

## Preview
- Read-only AvalonEdit, Consolas 12, line numbers, selectable/copyable.
- Shows exactly the text that will be saved.
- Rebuilt on a background thread with ~80 ms debounce; UI never blocks.

## Status bar
- Left: `N files · 3,214 lines · 201KB` (totals of merged output; MB at ≥ 1 MB).
- Right: transient messages.

## Output panel
- Name box + `Auto-name` toggle (default ON).
  - ON: read-only, live name from files in list order joined with `+`, original extensions kept, final `.txt` (e.g. `a.html+b.cs.txt`); invalid chars sanitized; > ~100 chars → first names + `+N more`.
  - OFF: user name (default `merged.txt`; `.txt` appended if no extension).
- Destination folder + `…` button (`OpenFolderDialog`). Default Desktop, then last used.
- Existing target → append ` (1)`, ` (2)`… without prompting.

## Actions
- `Save` (green, Ctrl+S): UTF-8 without BOM, `\r\n` endings, status `Saved: <name>`.
- `Show file`: enabled after first save; `explorer.exe /select,"<path>"`.

## Merge format
```
###Files has been merged together with FileMerger###

###index.html [224 lines, 2KB]
<content>

###init.cs [200 lines, 10KB]
<content>
```
- Header once; blank line between blocks; content always ends with exactly one added newline if missing.
- Lines: empty = 0, trailing newline adds none, singular `1 line`.
- Size (on-disk): `<1KB` → `NNB`, `<1MB` → KB, else MB; 1 decimal, trailing `.0` dropped, no space.

## Extras
- Title-bar pin = always on top (persisted). Nothing else.

## Settings & portability
- `FileMerger.settings.json` next to the exe; window bounds/state (clamped to screen), output folder, auto-name, custom name, always-on-top.
- Loaded synchronously, saved on change (debounced) and on exit; corrupt/read-only → silent defaults.
- No installer, no registry writes, no splash, no dialogs at startup.

## Publishing
- `FileMerger.exe`: framework-dependent single-file, win-x64, ReadyToRun.
- `FileMerger-selfcontained.exe`: self-contained single-file, native libs self-extract, no trimming.
