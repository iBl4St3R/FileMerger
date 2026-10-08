using System.Text;

namespace FileMerger.Core;

public static class MergeBuilder
{
    public const string NewLine = "\r\n";
    public const string Header = "###Files has been merged together with FileMerger###";

    public static string BlockHeader(MergeItem item) => $"###{item.Name} {item.Info}";

    /// <summary>Builds the merged text exactly as it is saved (CRLF). No items → empty text.</summary>
    public static MergeResult Build(IReadOnlyList<MergeItem> items, CancellationToken token = default)
    {
        if (items.Count == 0)
            return MergeResult.Empty;

        var capacity = Header.Length + 4;
        foreach (var item in items)
            capacity += item.Content.Length + item.Name.Length + 40;

        var sb = new StringBuilder(capacity);
        sb.Append(Header).Append(NewLine);
        foreach (var item in items)
        {
            token.ThrowIfCancellationRequested();
            sb.Append(NewLine);
            sb.Append(BlockHeader(item)).Append(NewLine);
            sb.Append(item.Content);
            if (item.Content.Length > 0 && !item.Content.EndsWith(NewLine, StringComparison.Ordinal))
                sb.Append(NewLine);
        }

        var text = sb.ToString();
        return new MergeResult(text, items.Count, LineCounter.Count(text), Encoding.UTF8.GetByteCount(text));
    }
}
