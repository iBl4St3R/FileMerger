using System.Text;
using FileMerger.Core;

namespace FileMerger.Tests;

public class TextFileReaderTests
{
    [Fact]
    public void ReadsUtf8AndNormalizes()
    {
        using var dir = new TempDir();
        var path = dir.File("a.js", "zażółć\nline2\n");
        var result = TextFileReader.Read(path);
        Assert.Null(result.SkipReason);
        Assert.Equal("zażółć\r\nline2\r\n", result.Item!.Content);
        Assert.Equal(2, result.Item.LineCount);
        Assert.Equal("a.js", result.Item.Name);
        Assert.Equal(new FileInfo(path).Length, result.Item.SizeBytes);
    }

    [Fact]
    public void StripsUtf8Bom() => Assert.Equal("hi", TextFileReader.Decode([0xEF, 0xBB, 0xBF, (byte)'h', (byte)'i']));

    [Fact]
    public void Utf16WithBomIsText()
    {
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("hello")).ToArray();
        Assert.Equal("hello", TextFileReader.Decode(bytes));
    }

    [Fact]
    public void Utf16BigEndianWithBomIsText()
    {
        var bytes = Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes("hej")).ToArray();
        Assert.Equal("hej", TextFileReader.Decode(bytes));
    }

    [Fact]
    public void NulByteMeansBinary() => Assert.Null(TextFileReader.Decode([0x89, (byte)'P', (byte)'N', (byte)'G', 0, 1, 2]));

    [Fact]
    public void NulAfterProbeIsText()
    {
        var bytes = Enumerable.Repeat((byte)'a', 9000).Append((byte)0).ToArray();
        Assert.NotNull(TextFileReader.Decode(bytes));
    }

    [Fact]
    public void InvalidUtf8FallsBackToAnsi()
    {
        var bytes = TextFileReader.AnsiEncoding().GetBytes("caf\u00e9");
        Assert.Equal("caf\u00e9", TextFileReader.Decode(bytes));
    }

    [Fact]
    public void SkipsBinaryFile()
    {
        using var dir = new TempDir();
        Assert.Equal("binary", TextFileReader.Read(dir.File("x.bin", [1, 0, 2])).SkipReason);
    }

    [Fact]
    public void SkipsMissingFile() => Assert.NotNull(TextFileReader.Read(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).SkipReason);

    [Fact]
    public void EmptyFileIsZeroLines()
    {
        using var dir = new TempDir();
        var result = TextFileReader.Read(dir.File("e.txt", Array.Empty<byte>()));
        Assert.Equal(0, result.Item!.LineCount);
    }
}
