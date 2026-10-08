namespace FileMerger.Core;

/// <summary>A file read into memory. <see cref="Content"/> is normalized to CRLF line endings.</summary>
public sealed record MergeItem(string FullPath, string Name, string Content, long SizeBytes, int LineCount)
{
    public string Info => $"[{LineCounter.Format(LineCount)}, {SizeFormatter.Format(SizeBytes)}]";
}
