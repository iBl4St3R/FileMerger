using System.Text;

namespace FileMerger.Core;

public static class OutputNameBuilder
{
    public const string DefaultName = "merged.txt";
    public const int MaxAutoLength = 100;
    private const string Extension = ".txt";
    private static readonly char[] InvalidChars = "<>:\"/\\|?*".ToCharArray();

    /// <summary>Joins names with '+', truncates to "+N more" past ~100 chars, appends ".txt".</summary>
    public static string AutoName(IReadOnlyList<string> fileNames)
    {
        if (fileNames.Count == 0)
            return DefaultName;

        var names = fileNames.Select(Sanitize).ToList();
        var joined = string.Join("+", names);
        if (joined.Length <= MaxAutoLength)
            return joined + Extension;

        var prefix = names[0];
        var used = 1;
        while (used < names.Count && prefix.Length + 1 + names[used].Length + MoreSuffix(names.Count - used - 1).Length <= MaxAutoLength)
            prefix += "+" + names[used++];

        var suffix = MoreSuffix(names.Count - used);
        if (prefix.Length + suffix.Length > MaxAutoLength)
            prefix = prefix[..Math.Max(1, MaxAutoLength - suffix.Length)];

        return prefix + suffix + Extension;
    }

    private static string MoreSuffix(int remaining) => remaining > 0 ? $"+{remaining} more" : string.Empty;

    /// <summary>Normalizes a user-typed name: sanitized, default when empty, ".txt" when no extension.</summary>
    public static string CustomName(string? input)
    {
        var name = Sanitize(input ?? string.Empty);
        if (name.Length == 0)
            return DefaultName;
        return Path.HasExtension(name) ? name : name + Extension;
    }

    /// <summary>Replaces characters invalid in Windows file names with '_' and trims trailing dots/spaces.</summary>
    public static string Sanitize(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name.Trim())
            sb.Append(c < 32 || Array.IndexOf(InvalidChars, c) >= 0 ? '_' : c);
        return sb.ToString().TrimEnd('.', ' ');
    }

    /// <summary>Returns a path in <paramref name="folder"/> that does not exist yet, appending " (1)", " (2)"…</summary>
    public static string UniquePath(string folder, string fileName, Func<string, bool>? exists = null)
    {
        exists ??= p => File.Exists(p) || Directory.Exists(p);
        var path = Path.Combine(folder, fileName);
        if (!exists(path))
            return path;

        var stem = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        for (var i = 1; ; i++)
        {
            path = Path.Combine(folder, $"{stem} ({i}){ext}");
            if (!exists(path))
                return path;
        }
    }
}
