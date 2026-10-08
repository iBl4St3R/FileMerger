using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FileMerger.Core;
using Microsoft.Win32;

namespace FileMerger;

/// <summary>UI glue only: window chrome, placement, drag &amp; drop, paste, dialogs.</summary>
public partial class MainWindow : Window
{
    private const string ReorderFormat = "FileMerger.MergeItem";
    private readonly MainViewModel _vm;
    private Point _dragStart;
    private MergeItem? _dragItem;

    public MainWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        LoadIcon();
        RestorePlacement(vm.Settings);

        Preview.Options.EnableTextDragDrop = false;
        Preview.Options.EnableHyperlinks = false;
        Preview.Options.EnableEmailHyperlinks = false;

        SourceInitialized += (_, _) => HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(NativeMethods.WindowProc);
        StateChanged += (_, _) => { UpdateMaximizeButton(); StorePlacement(); };
        LocationChanged += (_, _) => StorePlacement();
        SizeChanged += (_, _) => StorePlacement();
        Closing += (_, _) => { StorePlacement(); _vm.SaveSettingsNow(); };
        UpdateMaximizeButton();
    }

    private void LoadIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/icon.ico", UriKind.Absolute);
            var decoder = new IconBitmapDecoder(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            Icon = decoder.Frames[0];
            TitleIcon.Source = decoder.Frames.OrderBy(f => Math.Abs(f.PixelWidth - 32)).First();
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or FileFormatException or ArgumentException)
        {
            TitleIcon.Visibility = Visibility.Collapsed;
        }
    }

    // ---- Window placement ----

    private void RestorePlacement(AppSettings s)
    {
        if (s.Left is not double left || s.Top is not double top || s.Width is not double width || s.Height is not double height)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        Width = Math.Clamp(width, MinWidth, Math.Max(MinWidth, screen.Width));
        Height = Math.Clamp(height, MinHeight, Math.Max(MinHeight, screen.Height));
        Left = Math.Clamp(left, screen.Left, Math.Max(screen.Left, screen.Right - Width));
        Top = Math.Clamp(top, screen.Top, Math.Max(screen.Top, screen.Bottom - Height));
        WindowStartupLocation = WindowStartupLocation.Manual;
        if (s.Maximized)
            WindowState = WindowState.Maximized;
    }

    private void StorePlacement()
    {
        if (!IsLoaded)
            return;
        var s = _vm.Settings;
        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth, ActualHeight) : RestoreBounds;
        if (!bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
        {
            s.Left = bounds.Left;
            s.Top = bounds.Top;
            s.Width = bounds.Width;
            s.Height = bounds.Height;
        }
        if (WindowState != WindowState.Minimized)
            s.Maximized = WindowState == WindowState.Maximized;
        _vm.RequestSettingsSave();
    }

    // ---- Title bar ----

    private void UpdateMaximizeButton()
    {
        var maximized = WindowState == WindowState.Maximized;
        MaximizeButton.Content = maximized ? "" : "";
        MaximizeButton.ToolTip = maximized ? "Restore" : "Maximize";
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    // ---- Adding files ----

    private void AddFiles(IEnumerable<string> paths) => _ = _vm.AddFilesAsync(paths);

    private void DropZone_Click(object sender, MouseButtonEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Add files", Multiselect = true, Filter = "All files|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true)
            AddFiles(dialog.FileNames);
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose destination folder" };
        if (Directory.Exists(_vm.OutputFolder))
            dialog.InitialDirectory = _vm.OutputFolder;
        if (dialog.ShowDialog(this) == true)
            _vm.OutputFolder = dialog.FolderName;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control)
            return;
        if (e.Key == Key.V && TryPasteFiles())
        {
            e.Handled = true;
        }
        else if (e.Key == Key.S)
        {
            _vm.SaveCommand.Execute(null);
            e.Handled = true;
        }
    }

    private bool TryPasteFiles()
    {
        try
        {
            if (!Clipboard.ContainsFileDropList())
                return false;
            StringCollection files = Clipboard.GetFileDropList();
            AddFiles(files.Cast<string>().ToList());
            return true;
        }
        catch (ExternalException)
        {
            _vm.ShowStatus("Clipboard is busy, try again");
            return false;
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        SetDropHighlight(false);
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            AddFiles(files);
        e.Handled = true;
    }

    private void DropZone_DragEnter(object sender, DragEventArgs e) => SetDropHighlight(e.Data.GetDataPresent(DataFormats.FileDrop));

    private void DropZone_DragLeave(object sender, DragEventArgs e) => SetDropHighlight(false);

    private void SetDropHighlight(bool on)
    {
        DropBorder.SetResourceReference(Shape.StrokeProperty, on ? "AccentBrush" : "SurfaceHoverBrush");
        DropBorder.StrokeThickness = on ? 2 : 1.5;
        DropTitle.SetResourceReference(TextBlock.ForegroundProperty, on ? "AccentBrush" : "TextBrush");
        DropBorder.Effect = on && TryFindResource("AccentColor") is Color accent ? new DropShadowEffect { Color = accent, BlurRadius = 14, ShadowDepth = 0, Opacity = 0.6 } : null;
    }

    // ---- File list: reorder, delete ----

    private void FileList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragItem = null;
        var source = e.OriginalSource as DependencyObject;
        if (FindAncestor<ButtonBase>(source) is not null)
            return;
        _dragStart = e.GetPosition(FileList);
        _dragItem = FindAncestor<ListBoxItem>(source)?.DataContext as MergeItem;
    }

    private void FileList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragItem is null || e.LeftButton != MouseButtonState.Pressed)
            return;
        var delta = e.GetPosition(FileList) - _dragStart;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;
        var item = _dragItem;
        _dragItem = null;
        DragDrop.DoDragDrop(FileList, new DataObject(ReorderFormat, item), DragDropEffects.Move);
    }

    private void FileList_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(ReorderFormat) is not MergeItem dragged)
            return;
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
        if (FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject)?.DataContext is MergeItem target && !ReferenceEquals(target, dragged))
            _vm.Move(dragged, _vm.Items.IndexOf(target));
    }

    private void FileList_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(ReorderFormat))
            e.Handled = true;
    }

    private void FileList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete || FileList.SelectedItems.Count == 0)
            return;
        var index = FileList.SelectedIndex;
        _vm.Remove(FileList.SelectedItems.Cast<MergeItem>().ToList());
        if (_vm.Items.Count > 0)
        {
            FileList.SelectedIndex = Math.Min(index, _vm.Items.Count - 1);
            (FileList.ItemContainerGenerator.ContainerFromIndex(FileList.SelectedIndex) as ListBoxItem)?.Focus();
        }
        e.Handled = true;
    }

    private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node is not null and not T)
            node = node is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
        return node as T;
    }
}
