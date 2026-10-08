using FileMerger.Core;

namespace FileMerger.Tests;

public class MergeBuilderTests
{
    private static MergeItem Item(string name, string content, long size) => new("C:\\" + name, name, content, size, LineCounter.Count(content));

    [Fact]
    public void EmptyListProducesEmptyText()
    {
        var result = MergeBuilder.Build([]);
        Assert.Equal(string.Empty, result.Text);
        Assert.Equal(0, result.FileCount);
    }

    [Fact]
    public void BuildsExactFormat()
    {
        var items = new[] { Item("index.html", "<p>\r\n</p>\r\n", 2048), Item("init.cs", "class A {}", 10 * 1024), Item("empty.txt", "", 0) };
        var result = MergeBuilder.Build(items);
        var expected = "###Files has been merged together with FileMerger###\r\n\r\n###index.html [2 lines, 2KB]\r\n<p>\r\n</p>\r\n\r\n###init.cs [1 line, 10KB]\r\nclass A {}\r\n\r\n###empty.txt [0 lines, 0B]\r\n";
        Assert.Equal(expected, result.Text);
        Assert.Equal(3, result.FileCount);
        Assert.Equal(LineCounter.Count(expected), result.LineCount);
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(expected), result.ByteCount);
    }

    [Fact]
    public void DoesNotDoubleTrailingNewline()
    {
        var result = MergeBuilder.Build([Item("a.txt", "x\r\n", 3)]);
        Assert.EndsWith("###a.txt [1 line, 3B]\r\nx\r\n", result.Text);
        Assert.DoesNotContain("x\r\n\r\n", result.Text);
    }

    [Fact]
    public void SummaryFormatsTotals()
    {
        Assert.Equal("1 file · 1 line · 3B", new MergeResult("abc", 1, 1, 3).Summary);
        Assert.Equal("12 files · 3,214 lines · 201KB", new MergeResult("", 12, 3214, 201 * 1024).Summary);
        Assert.Equal("2 files · 9 lines · 1.5MB", new MergeResult("", 2, 9, 1536 * 1024).Summary);
    }
}
