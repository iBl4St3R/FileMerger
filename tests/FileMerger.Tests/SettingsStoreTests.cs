using FileMerger.Core;

namespace FileMerger.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void RoundTrips()
    {
        using var dir = new TempDir();
        var store = new SettingsStore(Path.Combine(dir.Path, SettingsStore.FileName));
        var settings = new AppSettings { Left = 10, Top = 20, Width = 480, Height = 760, OutputFolder = "C:\\out", AutoName = false, CustomName = "x.txt", AlwaysOnTop = true };
        Assert.True(store.Save(settings));

        var loaded = store.Load();
        Assert.Equal(10, loaded.Left);
        Assert.Equal("C:\\out", loaded.OutputFolder);
        Assert.False(loaded.AutoName);
        Assert.Equal("x.txt", loaded.CustomName);
        Assert.True(loaded.AlwaysOnTop);
    }

    [Fact]
    public void CorruptFileGivesDefaults()
    {
        using var dir = new TempDir();
        var store = new SettingsStore(dir.File(SettingsStore.FileName, "{ not json"));
        var loaded = store.Load();
        Assert.True(loaded.AutoName);
        Assert.Equal("merged.txt", loaded.CustomName);
    }

    [Fact]
    public void MissingFileGivesDefaults() => Assert.True(new SettingsStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "s.json")).Load().AutoName);

    [Fact]
    public void UnwritableLocationReturnsFalse() => Assert.False(new SettingsStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "s.json")).Save(new AppSettings()));
}
