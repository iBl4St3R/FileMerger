namespace FileMerger.Core;

public static class AddSummary
{
    public const string Duplicate = "duplicate";

    /// <summary>Formats e.g. <c>Added 3 files · skipped 2 binary, 1 duplicate</c>. Empty when nothing happened.</summary>
    public static string Format(int added, IReadOnlyDictionary<string, int> skipped)
    {
        var skips = string.Join(", ", skipped.Where(p => p.Value > 0).Select(p => $"{p.Value} {(p.Key == Duplicate && p.Value > 1 ? "duplicates" : p.Key)}"));
        var addedText = added == 1 ? "Added 1 file" : $"Added {added} files";
        if (skips.Length == 0)
            return added > 0 ? addedText : string.Empty;
        return added > 0 ? $"{addedText} · skipped {skips}" : $"Skipped {skips}";
    }
}
