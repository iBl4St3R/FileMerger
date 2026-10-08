using FileMerger.Core;

namespace FileMerger.Tests;

public class FileCollectorTests
{
    [Fact]
    public void ExpandsFoldersSkippingIgnored()
    {
        using var dir = new TempDir();
        var a = dir.File("a.txt", "a");
        var b = dir.File(Path.Combine("sub", "b.cs"), "b");
        dir.File(Path.Combine(".git", "config"), "x");
        dir.File(Path.Combine("bin", "x.dll"), "x");
        dir.File(Path.Combine("obj", "x.cs"), "x");
        dir.File(Path.Combine("node_modules", "p", "i.js"), "x");
        dir.File(Path.Combine(".vs", "s.json"), "x");

        var result = FileCollector.Collect([dir.Path], []);
        Assert.Equal([a, b], result.Files);
    }

    [Fact]
    public void IgnoresDuplicates()
    {
        using var dir = new TempDir();
        var a = dir.File("a.txt", "a");
        var b = dir.File("b.txt", "b");

        var result = FileCollector.Collect([a, b, a], [b]);
        Assert.Equal([a], result.Files);
        Assert.Equal(2, result.Duplicates);
    }

    [Fact]
    public void CountsMissing()
    {
        var result = FileCollector.Collect([Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))], []);
        Assert.Empty(result.Files);
        Assert.Equal(1, result.Missing);
    }
}
