# Decisions

- Kept the repo's existing VisualStudio `.gitignore` instead of regenerating it (equivalent to `dotnet new gitignore`).
- Pushed to `main` as instructed by the task, overriding the session's default feature branch.
- Empty file list merges to empty text (no header); Save is disabled until a file is added.
- Merged text and each file's cached content are normalized to CRLF; line counts use normalized content, size uses on-disk bytes.
- Status-bar size is the UTF-8 byte count of the merged output.
- Self-contained single-file uses `EnableCompressionInSingleFile` (~160MB → much smaller); framework-dependent stays uncompressed for fastest start.
- PDBs are embedded (`DebugType=embedded`) so publish folders contain only the exe.
- Whole window accepts file drops (not just the drop zone); the drop zone highlights on drag-over. The preview has text drag&drop disabled.
- List reordering is live while dragging (item moves under the cursor), no separate insertion marker.
- Empty preview shows a muted "Merged preview appears here" hint.
- Settings are saved 500 ms after the last change and on exit.
- `global.json` requires SDK ≥ 8.0.100 with `rollForward: latestMajor` (works with VS SDK 9/10; app still targets net8.0); `*.pubxml` un-ignored.
