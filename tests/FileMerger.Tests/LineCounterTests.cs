using FileMerger.Core;

namespace FileMerger.Tests;

public class LineCounterTests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("a", 1)]
    [InlineData("a\n", 1)]
    [InlineData("a\r\nb", 2)]
    [InlineData("a\r\nb\r\n", 2)]
    [InlineData("\n", 1)]
    [InlineData("a\n\n", 2)]
    public void Counts(string text, int expected) => Assert.Equal(expected, LineCounter.Count(text));

    [Theory]
    [InlineData(0, "0 lines")]
    [InlineData(1, "1 line")]
    [InlineData(3214, "3,214 lines")]
    public void FormatsCount(int lines, string expected) => Assert.Equal(expected, LineCounter.Format(lines));

    [Fact]
    public void NormalizesAllLineEndings() => Assert.Equal("a\r\nb\r\nc\r\nd", LineCounter.NormalizeToCrLf("a\nb\rc\r\nd"));
}
