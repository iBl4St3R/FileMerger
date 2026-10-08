using System.Globalization;
using System.Text;

namespace FileMerger.Core;

public sealed record ReadResult(MergeItem? Item, string? SkipReason);

public static class TextFileReader
{
    public const long MaxSizeBytes = 25L * 1024 * 1024;
    private const int BinaryProbeLength = 8 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    static TextFileReader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static ReadResult Read(string path)
    {
        var name = Path.GetFileName(path);
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
                return new ReadResult(null, "not found");
            if (info.Length > MaxSizeBytes)
                return new ReadResult(null, "larger than 25MB");

            var bytes = File.ReadAllBytes(path);
            var text = Decode(bytes);
            if (text is null)
                return new ReadResult(null, "binary");

            var content = LineCounter.NormalizeToCrLf(text);
            return new ReadResult(new MergeItem(Path.GetFullPath(path), name, content, bytes.LongLength, LineCounter.Count(content)), null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ReadResult(null, "unreadable");
        }
    }

    /// <summary>Decodes bytes as text; returns null for binary content.</summary>
    public static string? Decode(byte[] bytes)
    {
        var (bomEncoding, bomLength) = DetectBom(bytes);
        if (bomEncoding is not null)
            return bomEncoding.GetString(bytes, bomLength, bytes.Length - bomLength);

        if (Array.IndexOf(bytes, (byte)0, 0, Math.Min(bytes.Length, BinaryProbeLength)) >= 0)
            return null;

        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return AnsiEncoding().GetString(bytes);
        }
    }

    public static Encoding AnsiEncoding()
    {
        try
        {
            return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return Encoding.Latin1;
        }
    }

    private static (Encoding? Encoding, int Length) DetectBom(byte[] b)
    {
        if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xFE && b[2] == 0 && b[3] == 0)
            return (new UTF32Encoding(false, false), 4);
        if (b.Length >= 4 && b[0] == 0 && b[1] == 0 && b[2] == 0xFE && b[3] == 0xFF)
            return (new UTF32Encoding(true, false), 4);
        if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF)
            return (Encoding.UTF8, 3);
        if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE)
            return (Encoding.Unicode, 2);
        if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF)
            return (Encoding.BigEndianUnicode, 2);
        return (null, 0);
    }
}
