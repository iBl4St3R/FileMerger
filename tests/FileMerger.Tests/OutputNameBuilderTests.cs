using FileMerger.Core;

namespace FileMerger.Tests;

public class OutputNameBuilderTests
{
    [Fact]
    public void AutoNameJoinsInOrder() => Assert.Equal("index.html+init.cs+cos.txt+cos2.txt.txt", OutputNameBuilder.AutoName(["index.html", "init.cs", "cos.txt", "cos2.txt"]));

    [Fact]
    public void AutoNameEmptyIsDefault() => Assert.Equal("merged.txt", OutputNameBuilder.AutoName([]));

    [Fact]
    public void AutoNameTruncatesWithMoreSuffix()
    {
        var names = Enumerable.Range(1, 20).Select(i => $"file{i:00}.js").ToList();
        var name = OutputNameBuilder.AutoName(names);
        Assert.True(name.Length <= OutputNameBuilder.MaxAutoLength + 4, name);
        Assert.StartsWith("file01.js+file02.js+", name);
        Assert.Matches(@"\+\d+ more\.txt$", name);
        var shown = name.Split('+').Count(p => p.StartsWith("file"));
        Assert.EndsWith($"+{20 - shown} more.txt", name);
    }

    [Fact]
    public void AutoNameTruncatesHugeFirstName()
    {
        var name = OutputNameBuilder.AutoName([new string('a', 150), "b.txt"]);
        Assert.Equal(OutputNameBuilder.MaxAutoLength + 4, name.Length);
        Assert.EndsWith("+1 more.txt", name);
    }

    [Fact]
    public void AutoNameSanitizes() => Assert.Equal("a_b.txt.txt", OutputNameBuilder.AutoName(["a:b.txt"]));

    [Theory]
    [InlineData("", "merged.txt")]
    [InlineData("   ", "merged.txt")]
    [InlineData("notes", "notes.txt")]
    [InlineData("notes.md", "notes.md")]
    [InlineData("a<b>c?.log", "a_b_c_.log")]
    [InlineData("name...", "name.txt")]
    public void CustomNameNormalizes(string input, string expected) => Assert.Equal(expected, OutputNameBuilder.CustomName(input));

    [Fact]
    public void UniquePathAppendsCounter()
    {
        var taken = new HashSet<string> { Path.Combine("dir", "out.txt"), Path.Combine("dir", "out (1).txt") };
        Assert.Equal(Path.Combine("dir", "out (2).txt"), OutputNameBuilder.UniquePath("dir", "out.txt", taken.Contains));
        Assert.Equal(Path.Combine("dir", "new.txt"), OutputNameBuilder.UniquePath("dir", "new.txt", taken.Contains));
    }
}
