# Decisions

- Kept the repo's existing VisualStudio `.gitignore` instead of regenerating it (equivalent to `dotnet new gitignore`).
- Pushed to `main` as instructed by the task, overriding the session's default feature branch.
- Empty file list merges to empty text (no header); Save is disabled until a file is added.
- Merged text and each file's cached content are normalized to CRLF; line counts use normalized content, size uses on-disk bytes.
- Status-bar size is the UTF-8 byte count of the merged output.
