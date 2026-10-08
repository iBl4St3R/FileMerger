using System.Globalization;
using System.Text;

namespace FileMerger.Core;

public static class LineCounter
{
    /// <summary>Counts lines: empty text = 0, a trailing newline does not start a new line.</summary>
    public static int Count(string text)
    {
        if (text.Length == 0)
            return 0;
        var count = 0;
        foreach (var c in text)
        {
            if (c == '\n')
                count++;
        }
        return text[^1] == '\n' ? count : count + 1;
    }

    public static string Format(int lines) => lines == 1 ? "1 line" : $"{lines.ToString("N0", CultureInfo.InvariantCulture)} lines";

    /// <summary>Converts CR, LF and CRLF line endings to CRLF.</summary>
    public static string NormalizeToCrLf(string text)
    {
        if (text.IndexOf('\r') < 0 && text.IndexOf('\n') < 0)
            return text;
        var sb = new StringBuilder(text.Length + text.Length / 32);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                    i++;
                sb.Append("\r\n");
            }
            else if (c == '\n')
            {
                sb.Append("\r\n");
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
