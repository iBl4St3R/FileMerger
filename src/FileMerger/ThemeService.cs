using System.Windows;
using Microsoft.Win32;

namespace FileMerger;

/// <summary>Follows the Windows app mode (light/dark) by swapping the theme dictionary at index 0 of the app resources.</summary>
public static class ThemeService
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private static Application? _app;
    private static bool? _isLight;

    public static void Start(Application app)
    {
        _app = app;
        Apply();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public static void Stop() => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

    public static bool IsSystemLight()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue("AppsUseLightTheme") is not int value || value != 0;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or System.IO.IOException)
        {
            return true;
        }
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.VisualStyle)
            _app?.Dispatcher.BeginInvoke(Apply);
    }

    private static void Apply()
    {
        if (_app is null)
            return;
        var light = IsSystemLight();
        if (light == _isLight)
            return;

        var theme = new ResourceDictionary { Source = new Uri($"pack://application:,,,/Themes/{(light ? "Light" : "Dark")}.xaml", UriKind.Absolute) };
        var dictionaries = _app.Resources.MergedDictionaries;
        if (_isLight is null)
            dictionaries.Insert(0, theme);
        else
            dictionaries[0] = theme;
        _isLight = light;
    }
}
