using FileMerger.Core;

namespace FileMerger.Tests;

public class AddSummaryTests
{
    [Fact]
    public void FormatsCombinations()
    {
        Assert.Equal(string.Empty, AddSummary.Format(0, new Dictionary<string, int>()));
        Assert.Equal("Added 1 file", AddSummary.Format(1, new Dictionary<string, int>()));
        Assert.Equal("Added 3 files · skipped 2 binary, 1 duplicate", AddSummary.Format(3, new Dictionary<string, int> { ["binary"] = 2, [AddSummary.Duplicate] = 1 }));
        Assert.Equal("Skipped 2 duplicates", AddSummary.Format(0, new Dictionary<string, int> { [AddSummary.Duplicate] = 2 }));
    }
}
