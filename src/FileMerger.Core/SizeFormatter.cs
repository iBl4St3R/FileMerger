using System.Globalization;

namespace FileMerger.Core;

public static class SizeFormatter
{
    private const double Kb = 1024;
    private const double Mb = 1024 * 1024;

    /// <summary>Formats bytes as <c>NNB</c>, <c>NKB</c> or <c>NMB</c>, 1 decimal max, trailing .0 dropped, no space.</summary>
    public static string Format(long bytes)
    {
        if (bytes < Kb)
            return bytes.ToString(CultureInfo.InvariantCulture) + "B";
        if (bytes < Mb)
            return Round(bytes / Kb) + "KB";
        return Round(bytes / Mb) + "MB";
    }

    private static string Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.#", CultureInfo.InvariantCulture);
}
