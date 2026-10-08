using System.Globalization;

namespace FileMerger.Core;

public sealed record MergeResult(string Text, int FileCount, int LineCount, long ByteCount)
{
    public static readonly MergeResult Empty = new(string.Empty, 0, 0, 0);

    /// <summary>Status-bar summary, e.g. <c>3 files · 3,214 lines · 201KB</c>.</summary>
    public string Summary => $"{(FileCount == 1 ? "1 file" : FileCount.ToString("N0", CultureInfo.InvariantCulture) + " files")} · {LineCounter.Format(LineCount)} · {SizeFormatter.Format(ByteCount)}";
}
