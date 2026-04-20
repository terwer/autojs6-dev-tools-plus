using System.Collections.ObjectModel;
using App.Avalonia.Models;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;

namespace App.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        AddLog("工作台", "Avalonia 主工作台布局已初始化。");
        AddLog("模式", "当前模式：图像模式。");
    }

    public string WindowTitle => "AutoJS6 可视化工作台";

    public ObservableCollection<DeviceListItemViewModel> Devices { get; } = [];

    public ObservableCollection<WidgetTreeNodeViewModel> WidgetNodes { get; } = [];

    public ObservableCollection<WorkbenchLogEntryViewModel> Logs { get; } = [];

    public ObservableCollection<CanvasOverlayRect> CanvasOverlays { get; } = [];

    [ObservableProperty]
    private WorkbenchMode _currentMode = WorkbenchMode.Image;

    [ObservableProperty]
    private DeviceListItemViewModel? _selectedDevice;

    [ObservableProperty]
    private bool _isWidgetBoundsVisible = true;

    [ObservableProperty]
    private bool _showTextWidgets = true;

    [ObservableProperty]
    private bool _showButtonWidgets = true;

    [ObservableProperty]
    private bool _showImageWidgets = true;

    [ObservableProperty]
    private bool _showOtherWidgets = true;

    [ObservableProperty]
    private double _widgetOverlayOpacity = 0.55d;

    [ObservableProperty]
    private bool _isTemplateSourceCurrent = true;

    [ObservableProperty]
    private bool _isTemplateSourceBrowse;

    [ObservableProperty]
    private bool _isScreenshotSourceCurrent = true;

    [ObservableProperty]
    private bool _isScreenshotSourceBrowse;

    [ObservableProperty]
    private bool _searchAllScreen;

    [ObservableProperty]
    private bool _isLogPanelOpen;

    [ObservableProperty]
    private bool _isCropModeEnabled;

    [ObservableProperty]
    private string _widgetTreeSearchText = string.Empty;

    [ObservableProperty]
    private string _operationInputText = "[等待裁剪...]";

    [ObservableProperty]
    private double _matchThreshold = 0.84d;

    [ObservableProperty]
    private string _matchResultSummary = "结果：等待执行匹配测试";

    [ObservableProperty]
    private string _templateDirectoryText = "未设置模板目录";

    [ObservableProperty]
    private string _templateName = string.Empty;

    [ObservableProperty]
    private string _propertyClassName = "-";

    [ObservableProperty]
    private string _propertyText = "-";

    [ObservableProperty]
    private string _propertyId = "-";

    [ObservableProperty]
    private string _propertyBounds = "-";

    [ObservableProperty]
    private string _propertyPackage = "-";

    [ObservableProperty]
    private string _propertyClickable = "-";

    [ObservableProperty]
    private string _widgetSelectionHint = "尚未选中控件。进入控件模式后，可在中央画布中点击控件边界框查看属性。";

    [ObservableProperty]
    private string _selectorValidationReport = "尚未执行选择器验证。";

    [ObservableProperty]
    private string _alignmentValidationReport = "尚未执行坐标对齐验证。";

    [ObservableProperty]
    private string _canvasResolutionText = "-";

    [ObservableProperty]
    private string _canvasZoomText = "100%";

    [ObservableProperty]
    private string _canvasCropText = "-";

    [ObservableProperty]
    private string _canvasOffsetText = "(0, 0)";

    [ObservableProperty]
    private string _canvasRotationText = "0°";

    [ObservableProperty]
    private string _canvasPointerText = "-";

    [ObservableProperty]
    private string _footerStatusText = "就绪";

    [ObservableProperty]
    private Bitmap? _canvasBitmap;

    [ObservableProperty]
    private CropRegion? _cropRegion;

    public bool IsImageMode => CurrentMode == WorkbenchMode.Image;

    public bool IsWidgetMode => CurrentMode == WorkbenchMode.Widget;

    public bool HasCanvasContent => CanvasBitmap is not null;

    public bool IsCanvasEmpty => !HasCanvasContent;

    public bool HasDevices => Devices.Count > 0;

    public bool IsDeviceListEmpty => !HasDevices;

    public bool HasWidgetNodes => WidgetNodes.Count > 0;

    public bool IsWidgetTreeEmpty => !HasWidgetNodes;

    public bool ArePrimaryWorkflowActionsEnabled => SelectedDevice is not null && HasCanvasContent;

    public string CropButtonText => IsCropModeEnabled ? "退出裁剪" : "开启裁剪";

    public string CurrentDeviceText =>
        SelectedDevice is null
            ? "当前设备：尚未选择"
            : $"当前设备：{SelectedDevice.Serial}{(string.IsNullOrWhiteSpace(SelectedDevice.Model) ? string.Empty : $" · {SelectedDevice.Model}")}";

    public string CanvasPlaceholderTitle =>
        IsImageMode ? "画布尚未加载截图" : "画布尚未加载控件截图";

    public string CanvasPlaceholderDescription =>
        IsImageMode
            ? "选择设备后点击截屏，或直接载入本地图片开始工作。"
            : "切换到控件模式后，请先截屏并拉取 UI 树，再查看控件边界框与节点树。";

    public string WorkspaceStatusLine =>
        $"模式：{(IsImageMode ? "图像模式" : "控件模式")}    分辨率：{CanvasResolutionText}    缩放：{CanvasZoomText}    偏移：{CanvasOffsetText}    旋转：{CanvasRotationText}    坐标：{CanvasPointerText}    裁剪区域：{CanvasCropText}";

    public string FooterSummary =>
        $"缩放：{CanvasZoomText}   偏移：{CanvasOffsetText}   旋转：{CanvasRotationText}   坐标：{CanvasPointerText}   分辨率：{CanvasResolutionText}   当前模式：{(IsImageMode ? "图像模式" : "控件模式")}";

    partial void OnCurrentModeChanged(WorkbenchMode value)
    {
        if (value == WorkbenchMode.Widget)
        {
            IsCropModeEnabled = false;
        }

        OnPropertyChanged(nameof(IsImageMode));
        OnPropertyChanged(nameof(IsWidgetMode));
        OnPropertyChanged(nameof(CanvasPlaceholderTitle));
        OnPropertyChanged(nameof(CanvasPlaceholderDescription));
        OnPropertyChanged(nameof(WorkspaceStatusLine));
        OnPropertyChanged(nameof(FooterSummary));
        AddLog("模式", $"切换到{(value == WorkbenchMode.Image ? "图像模式" : "控件模式")}。");
        FooterStatusText = value == WorkbenchMode.Image ? "已切换到图像模式" : "已切换到控件模式";
    }

    partial void OnSelectedDeviceChanged(DeviceListItemViewModel? value)
    {
        foreach (var device in Devices)
        {
            device.IsSelected = ReferenceEquals(device, value);
        }

        OnPropertyChanged(nameof(CurrentDeviceText));
        OnPropertyChanged(nameof(ArePrimaryWorkflowActionsEnabled));

        if (value is not null)
        {
            AddLog("设备", $"已选中设备：{value.Serial}");
            FooterStatusText = $"当前设备已切换为 {value.Serial}";
        }
    }

    partial void OnCanvasBitmapChanged(Bitmap? value)
    {
        OnPropertyChanged(nameof(HasCanvasContent));
        OnPropertyChanged(nameof(IsCanvasEmpty));
        OnPropertyChanged(nameof(ArePrimaryWorkflowActionsEnabled));

        if (value is not null)
        {
            CanvasResolutionText = $"{value.PixelSize.Width}x{value.PixelSize.Height}";
            AddLog("画布", $"已装载画布位图：{CanvasResolutionText}");
        }
        else
        {
            CanvasResolutionText = "-";
            CanvasZoomText = "100%";
            CanvasOffsetText = "(0, 0)";
            CanvasRotationText = "0°";
            CanvasPointerText = "-";
            CropRegion = null;
            IsCropModeEnabled = false;
        }

        OnPropertyChanged(nameof(WorkspaceStatusLine));
        OnPropertyChanged(nameof(FooterSummary));
    }

    partial void OnCropRegionChanged(CropRegion? value)
    {
        CanvasCropText = value is null ? "-" : $"[{value.X}, {value.Y}, {value.Width}, {value.Height}]";
        OperationInputText = value is null ? "[等待裁剪...]" : $"[{value.X}, {value.Y}, {value.Width}, {value.Height}]";
        OnPropertyChanged(nameof(WorkspaceStatusLine));

        if (value is not null)
        {
            FooterStatusText = $"裁剪区域已更新：{value.Width}x{value.Height}";
        }
    }

    partial void OnIsCropModeEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(CropButtonText));
        if (CurrentMode == WorkbenchMode.Image)
        {
            FooterStatusText = value ? "已进入裁剪模式，左键拖拽创建或调整裁剪框" : "已退出裁剪模式，左键拖拽恢复平移";
        }
    }

    partial void OnIsTemplateSourceCurrentChanged(bool value)
    {
        if (value)
        {
            IsTemplateSourceBrowse = false;
        }
        else if (!IsTemplateSourceBrowse)
        {
            IsTemplateSourceBrowse = true;
        }
    }

    partial void OnIsTemplateSourceBrowseChanged(bool value)
    {
        if (value)
        {
            IsTemplateSourceCurrent = false;
        }
        else if (!IsTemplateSourceCurrent)
        {
            IsTemplateSourceCurrent = true;
        }
    }

    partial void OnIsScreenshotSourceCurrentChanged(bool value)
    {
        if (value)
        {
            IsScreenshotSourceBrowse = false;
        }
        else if (!IsScreenshotSourceBrowse)
        {
            IsScreenshotSourceBrowse = true;
        }
    }

    partial void OnIsScreenshotSourceBrowseChanged(bool value)
    {
        if (value)
        {
            IsScreenshotSourceCurrent = false;
        }
        else if (!IsScreenshotSourceCurrent)
        {
            IsScreenshotSourceCurrent = true;
        }
    }

    partial void OnWidgetTreeSearchTextChanged(string value)
    {
        _ = RebuildFlatWidgetTreeAsync(value);
    }

    [RelayCommand]
    private void SwitchToImageMode()
    {
        CurrentMode = WorkbenchMode.Image;
    }

    [RelayCommand]
    private void SwitchToWidgetMode()
    {
        CurrentMode = WorkbenchMode.Widget;
    }

    [RelayCommand]
    private void SelectDevice(DeviceListItemViewModel? device)
    {
        if (device is null)
        {
            return;
        }

        SelectedDevice = device;
    }

    [RelayCommand]
    private void ToggleLogPanel()
    {
        IsLogPanelOpen = !IsLogPanelOpen;
        FooterStatusText = IsLogPanelOpen ? "调试日志已展开" : "调试日志已收起";
    }

    [RelayCommand]
    private void ToggleCropMode()
    {
        if (!HasCanvasContent || !IsImageMode)
        {
            return;
        }

        IsCropModeEnabled = !IsCropModeEnabled;
    }

    public void UpdateCanvasViewportState(double zoomFactor, double panX, double panY, int rotationDegrees)
    {
        CanvasZoomText = $"{zoomFactor * 100:F0}%";
        CanvasOffsetText = $"({panX:F0}, {panY:F0})";
        CanvasRotationText = $"{rotationDegrees}°";
        OnPropertyChanged(nameof(WorkspaceStatusLine));
        OnPropertyChanged(nameof(FooterSummary));
    }

    public void UpdateCanvasPointerState(int? pixelX, int? pixelY, bool isCrosshairVisible)
    {
        CanvasPointerText = pixelX.HasValue && pixelY.HasValue ? $"({pixelX.Value}, {pixelY.Value})" : "-";

        if (isCrosshairVisible && pixelX.HasValue && pixelY.HasValue)
        {
            FooterStatusText = $"十字准线锁定：({pixelX.Value}, {pixelY.Value})";
        }

        OnPropertyChanged(nameof(WorkspaceStatusLine));
        OnPropertyChanged(nameof(FooterSummary));
    }

    private void AddLog(string category, string message)
    {
        Logs.Insert(0, new WorkbenchLogEntryViewModel
        {
            TimeText = DateTime.Now.ToString("HH:mm:ss"),
            Category = category,
            Message = message
        });
    }
}
