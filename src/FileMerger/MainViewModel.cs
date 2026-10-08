using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Threading;
using FileMerger.Core;
using ICSharpCode.AvalonEdit.Document;

namespace FileMerger;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly TimeSpan PreviewDebounce = TimeSpan.FromMilliseconds(80);

    private readonly SettingsStore _store;
    private readonly SemaphoreSlim _addLock = new(1, 1);
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(6) };
    private readonly DispatcherTimer _settingsTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private CancellationTokenSource? _previewCts;
    private int _previewVersion;
    private TextDocument _previewDocument = new();
    private string _summary = MergeResult.Empty.Summary;
    private string _statusMessage = string.Empty;
    private string _outputName;
    private string? _lastSavedPath;
    private bool _saving;

    public MainViewModel(SettingsStore store, AppSettings settings)
    {
        _store = store;
        Settings = settings;
        if (string.IsNullOrWhiteSpace(Settings.OutputFolder) || !Directory.Exists(Settings.OutputFolder))
            Settings.OutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        _outputName = settings.AutoName ? OutputNameBuilder.AutoName([]) : settings.CustomName;

        SaveCommand = new RelayCommand(_ => _ = SaveAsync(), _ => Items.Count > 0 && !_saving);
        ShowFileCommand = new RelayCommand(_ => ShowFile(), _ => _lastSavedPath is not null);
        ClearCommand = new RelayCommand(_ => Items.Clear(), _ => Items.Count > 0);
        RemoveCommand = new RelayCommand(p => { if (p is MergeItem item) Items.Remove(item); });

        _statusTimer.Tick += (_, _) => { _statusTimer.Stop(); StatusMessage = string.Empty; };
        _settingsTimer.Tick += (_, _) => SaveSettingsNow();
        Items.CollectionChanged += (_, _) => OnItemsChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AppSettings Settings { get; }
    public ObservableCollection<MergeItem> Items { get; } = [];
    public RelayCommand SaveCommand { get; }
    public RelayCommand ShowFileCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand RemoveCommand { get; }

    public string FilesHeader => $"FILES · {Items.Count}";
    public bool IsEmpty => Items.Count == 0;

    public TextDocument PreviewDocument
    {
        get => _previewDocument;
        private set => Set(ref _previewDocument, value);
    }

    public string Summary
    {
        get => _summary;
        private set => Set(ref _summary, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => Set(ref _statusMessage, value);
    }

    public bool AlwaysOnTop
    {
        get => Settings.AlwaysOnTop;
        set
        {
            if (Settings.AlwaysOnTop == value)
                return;
            Settings.AlwaysOnTop = value;
            OnPropertyChanged();
            RequestSettingsSave();
        }
    }

    public bool AutoName
    {
        get => Settings.AutoName;
        set
        {
            if (Settings.AutoName == value)
                return;
            Settings.AutoName = value;
            OnPropertyChanged();
            OutputNameValue = value ? OutputNameBuilder.AutoName(ItemNames()) : Settings.CustomName;
            RequestSettingsSave();
        }
    }

    /// <summary>Name box text. Read-only (auto-generated) while <see cref="AutoName"/> is on.</summary>
    public string OutputName
    {
        get => _outputName;
        set
        {
            if (AutoName || _outputName == value)
                return;
            OutputNameValue = value;
            Settings.CustomName = value;
            RequestSettingsSave();
        }
    }

    public string OutputFolder
    {
        get => Settings.OutputFolder ?? string.Empty;
        set
        {
            if (Settings.OutputFolder == value)
                return;
            Settings.OutputFolder = value;
            OnPropertyChanged();
            RequestSettingsSave();
        }
    }

    private string OutputNameValue
    {
        set
        {
            _outputName = value;
            OnPropertyChanged(nameof(OutputName));
        }
    }

    public async Task AddFilesAsync(IEnumerable<string> paths)
    {
        var list = paths.ToList();
        if (list.Count == 0)
            return;

        await _addLock.WaitAsync();
        try
        {
            var known = Items.Select(i => i.FullPath).ToList();
            var (added, skipped) = await Task.Run(() => ReadFiles(list, known));
            foreach (var item in added)
                Items.Add(item);
            ShowStatus(AddSummary.Format(added.Count, skipped));
        }
        finally
        {
            _addLock.Release();
        }
    }

    public void Remove(IReadOnlyList<MergeItem> items)
    {
        foreach (var item in items)
            Items.Remove(item);
    }

    public void Move(MergeItem item, int newIndex)
    {
        var oldIndex = Items.IndexOf(item);
        if (oldIndex >= 0 && newIndex >= 0 && newIndex < Items.Count && oldIndex != newIndex)
            Items.Move(oldIndex, newIndex);
    }

    public void ShowStatus(string message)
    {
        if (message.Length == 0)
            return;
        StatusMessage = message;
        _statusTimer.Stop();
        _statusTimer.Start();
    }

    public void RequestSettingsSave()
    {
        _settingsTimer.Stop();
        _settingsTimer.Start();
    }

    public void SaveSettingsNow()
    {
        _settingsTimer.Stop();
        _store.Save(Settings);
    }

    private static (List<MergeItem> Added, Dictionary<string, int> Skipped) ReadFiles(List<string> paths, List<string> known)
    {
        var collected = FileCollector.Collect(paths, known);
        var added = new List<MergeItem>(collected.Files.Count);
        var skipped = new Dictionary<string, int>();
        void Skip(string reason, int count = 1) => skipped[reason] = skipped.GetValueOrDefault(reason) + count;

        if (collected.Duplicates > 0)
            Skip(AddSummary.Duplicate, collected.Duplicates);
        if (collected.Missing > 0)
            Skip("not found", collected.Missing);

        foreach (var file in collected.Files)
        {
            var result = TextFileReader.Read(file);
            if (result.Item is not null)
                added.Add(result.Item);
            else
                Skip(result.SkipReason ?? "unreadable");
        }
        return (added, skipped);
    }

    private List<string> ItemNames() => Items.Select(i => i.Name).ToList();

    private void OnItemsChanged()
    {
        OnPropertyChanged(nameof(FilesHeader));
        OnPropertyChanged(nameof(IsEmpty));
        if (AutoName)
            OutputNameValue = OutputNameBuilder.AutoName(ItemNames());
        SaveCommand.RaiseCanExecuteChanged();
        ClearCommand.RaiseCanExecuteChanged();
        SchedulePreview();
    }

    private async void SchedulePreview()
    {
        _previewCts?.Cancel();
        var cts = _previewCts = new CancellationTokenSource();
        var version = ++_previewVersion;
        var items = Items.ToArray();
        try
        {
            var (result, document) = await Task.Run(async () =>
            {
                await Task.Delay(PreviewDebounce, cts.Token);
                var merged = MergeBuilder.Build(items, cts.Token);
                var doc = new TextDocument(merged.Text);
                doc.SetOwnerThread(null);
                return (merged, doc);
            }, cts.Token);

            if (version != _previewVersion)
                return;
            document.SetOwnerThread(Thread.CurrentThread);
            PreviewDocument = document;
            Summary = result.Summary;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task SaveAsync()
    {
        if (Items.Count == 0 || _saving)
            return;

        var folder = OutputFolder;
        if (!Directory.Exists(folder))
        {
            ShowStatus("Destination folder not found");
            return;
        }

        var items = Items.ToArray();
        var fileName = AutoName ? OutputNameBuilder.AutoName(ItemNames()) : OutputNameBuilder.CustomName(OutputName);
        _saving = true;
        SaveCommand.RaiseCanExecuteChanged();
        try
        {
            var path = await Task.Run(() =>
            {
                var target = OutputNameBuilder.UniquePath(folder, fileName);
                File.WriteAllText(target, MergeBuilder.Build(items).Text, Utf8NoBom);
                return target;
            });
            _lastSavedPath = path;
            ShowFileCommand.RaiseCanExecuteChanged();
            ShowStatus($"Saved: {Path.GetFileName(path)}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowStatus($"Save failed: {ex.Message}");
        }
        finally
        {
            _saving = false;
            SaveCommand.RaiseCanExecuteChanged();
        }
    }

    private void ShowFile()
    {
        if (_lastSavedPath is null)
            return;
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_lastSavedPath}\"") { UseShellExecute = false })?.Dispose();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            ShowStatus("Could not open Explorer");
        }
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        OnPropertyChanged(name);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
