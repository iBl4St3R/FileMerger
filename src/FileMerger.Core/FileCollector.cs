namespace FileMerger.Core;

public sealed record CollectResult(IReadOnlyList<string> Files, int Duplicates, int Missing);

public static class FileCollector
{
    public static readonly IReadOnlySet<string> IgnoredFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", "bin", "obj", "node_modules", ".vs" };

    public static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    /// <summary>Expands folders recursively and removes paths already in <paramref name="known"/> or repeated in the input.</summary>
    public static CollectResult Collect(IEnumerable<string> paths, IEnumerable<string> known)
    {
        var seen = new HashSet<string>(known, PathComparer);
        var files = new List<string>();
        int duplicates = 0, missing = 0;

        void Add(string file)
        {
            if (seen.Add(file))
                files.Add(file);
            else
                duplicates++;
        }

        foreach (var raw in paths)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            string path;
            try
            {
                path = Path.GetFullPath(raw);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException)
            {
                missing++;
                continue;
            }

            if (File.Exists(path))
                Add(path);
            else if (Directory.Exists(path))
                foreach (var file in EnumerateFolder(path))
                    Add(file);
            else
                missing++;
        }

        return new CollectResult(files, duplicates, missing);
    }

    private static IEnumerable<string> EnumerateFolder(string root)
    {
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            string[] files, dirs;
            try
            {
                files = Directory.GetFiles(dir);
                dirs = Directory.GetDirectories(dir);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            Array.Sort(files, PathComparer);
            foreach (var file in files)
                yield return file;

            Array.Sort(dirs, PathComparer);
            for (var i = dirs.Length - 1; i >= 0; i--)
            {
                if (!IgnoredFolders.Contains(Path.GetFileName(dirs[i])))
                    stack.Push(dirs[i]);
            }
        }
    }
}
