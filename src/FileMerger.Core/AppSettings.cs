namespace FileMerger.Core;

public sealed class AppSettings
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public bool Maximized { get; set; }
    public string? OutputFolder { get; set; }
    public bool AutoName { get; set; } = true;
    public string CustomName { get; set; } = OutputNameBuilder.DefaultName;
    public bool AlwaysOnTop { get; set; }
}
