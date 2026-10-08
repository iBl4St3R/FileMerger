using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileMerger.Core;

[JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;

/// <summary>Loads/saves settings as JSON. Never throws: corrupt or unreadable → defaults, unwritable → ignored.</summary>
public sealed class SettingsStore(string path)
{
    public const string FileName = "FileMerger.settings.json";

    public string Path { get; } = path;

    public static SettingsStore NextToExecutable()
    {
        var dir = System.IO.Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        return new SettingsStore(System.IO.Path.Combine(dir, FileName));
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(Path))
                return new AppSettings();
            var settings = JsonSerializer.Deserialize(File.ReadAllText(Path), SettingsJsonContext.Default.AppSettings) ?? new AppSettings();
            settings.CustomName ??= OutputNameBuilder.DefaultName;
            return settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return new AppSettings();
        }
    }

    public bool Save(AppSettings settings)
    {
        try
        {
            File.WriteAllText(Path, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.AppSettings));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }
}
