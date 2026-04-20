using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using App.Avalonia.Models;
using App.Avalonia.ViewModels;
using Core.Abstractions.Desktop;
using Core.Models.Desktop;

namespace App.Avalonia.Views;

public partial class MainWindow : Window
{
    private const string WindowId = "main-window";
    private readonly IWindowStateService? _windowStateService;
    private readonly IClipboardService? _clipboardService;
    private MainWindowViewModel? _viewModel;

    public MainWindow()
        : this(null, null)
    {
    }

    public MainWindow(IWindowStateService? windowStateService, IClipboardService? clipboardService)
    {
        _windowStateService = windowStateService;
        _clipboardService = clipboardService;
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
        RotateImageButton.Click += OnRotateButtonClick;
        RotateWidgetButton.Click += OnRotateButtonClick;
        WorkbenchCanvas.ViewportChanged += OnCanvasViewportChanged;
        WorkbenchCanvas.PointerInfoChanged += OnCanvasPointerInfoChanged;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CodePreviewRequested -= OnCodePreviewRequested;
        }

        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel is not null)
        {
            _viewModel.CodePreviewRequested += OnCodePreviewRequested;
        }

        base.OnDataContextChanged(e);
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_windowStateService is not null)
        {
            var snapshot = await _windowStateService.RestoreAsync(WindowId);
            if (snapshot is not null)
            {
                Width = snapshot.Width;
                Height = snapshot.Height;
                Position = new PixelPoint((int)snapshot.X, (int)snapshot.Y);
                WindowState = snapshot.State switch
                {
                    DesktopWindowState.Maximized => WindowState.Maximized,
                    DesktopWindowState.FullScreen => WindowState.FullScreen,
                    _ => WindowState.Normal
                };
            }
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_windowStateService is null)
        {
            return;
        }

        var state = WindowState == WindowState.Minimized ? DesktopWindowState.Normal : MapWindowState(WindowState);
        var snapshot = new WindowStateSnapshot(Width, Height, Position.X, Position.Y, state);
        _windowStateService.PersistAsync(WindowId, snapshot).GetAwaiter().GetResult();
    }

    private void OnRotateButtonClick(object? sender, RoutedEventArgs e)
    {
        WorkbenchCanvas.RotateClockwise90();
    }

    private void OnCanvasViewportChanged(object? sender, CanvasViewportChangedEventArgs e)
    {
        _viewModel?.UpdateCanvasViewportState(e.ZoomFactor, e.PanOffset.X, e.PanOffset.Y, e.RotationDegrees);
    }

    private void OnCanvasPointerInfoChanged(object? sender, CanvasPointerInfoChangedEventArgs e)
    {
        _viewModel?.UpdateCanvasPointerState(e.PixelX, e.PixelY, e.IsCrosshairVisible);
    }

    private async void OnCodePreviewRequested(object? sender, string code)
    {
        var previewWindow = new CodePreviewWindow(code, _clipboardService)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        await previewWindow.ShowDialog(this);
    }

    private static DesktopWindowState MapWindowState(WindowState state)
    {
        return state switch
        {
            WindowState.Maximized => DesktopWindowState.Maximized,
            WindowState.FullScreen => DesktopWindowState.FullScreen,
            WindowState.Minimized => DesktopWindowState.Minimized,
            _ => DesktopWindowState.Normal
        };
    }
}
