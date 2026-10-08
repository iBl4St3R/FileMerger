using FileMerger.Core;

namespace FileMerger.Tests;

public class SizeFormatterTests
{
    [Theory]
    [InlineData(0, "0B")]
    [InlineData(1023, "1023B")]
    [InlineData(1024, "1KB")]
    [InlineData(2048, "2KB")]
    [InlineData(1536, "1.5KB")]
    [InlineData(10 * 1024, "10KB")]
    [InlineData(1024 * 1024 - 1, "1024KB")]
    [InlineData(1024 * 1024, "1MB")]
    [InlineData(1468006, "1.4MB")]
    [InlineData(25L * 1024 * 1024, "25MB")]
    public void Formats(long bytes, string expected) => Assert.Equal(expected, SizeFormatter.Format(bytes));
}
