using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using App.Avalonia.Models;
using App.Avalonia.Services;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Abstractions;
using Core.Abstractions.Desktop;
using Core.Models;
using Core.Models.Desktop;

namespace App.Avalonia.ViewModels;

public partial class MainWindowViewModel
{
    private readonly IAdbService? _adbService;
    private readonly IImageProcessingService? _imageProcessingService;
    private readonly IOpenCVMatchService? _openCvMatchService;
    private readonly IUiDumpParser? _uiDumpParser;
    private readonly ICodeGenerator? _codeGenerator;
    private readonly IClipboardService? _clipboardService;
    private readonly IMessageService? _messageService;
    private readonly IFileSaveDialogService? _fileSaveDialogService;
    private readonly IWorkbenchFilePickerService? _filePickerService;
    private readonly List<CanvasOverlayRect> _widgetOverlays = [];
    private readonly List<CanvasOverlayRect> _matchOverlays = [];
    private readonly HashSet<WidgetNode> _expandedWidgetNodes = [];
    private readonly List<MatchResult> _cachedMatchCandidates = [];
    private IReadOnlyList<WidgetNode> _currentFilteredWidgetNodes = Array.Empty<WidgetNode>();
    private byte[]? _currentScreenshotBytes;
    private byte[]? _currentTemplateBytes;
    private string? _currentScreenshotPath;
    private string? _currentTemplatePath;
    private string? _lastExportedTemplatePath;
    private WidgetNode? _widgetRoot;
    private MatchResult? _cachedBestMatch;
    private string? _generatedCode;

    public MainWindowViewModel(
        IAdbService adbService,
        IImageProcessingService imageProcessingService,
        IOpenCVMatchService openCvMatchService,
        IUiDumpParser uiDumpParser,
        ICodeGenerator codeGenerator,
        IClipboardService clipboardService,
        IMessageService messageService,
        IFileSaveDialogService fileSaveDialogService,
        IWorkbenchFilePickerService filePickerService)
        : this()
    {
        _adbService = adbService;
        _imageProcessingService = imageProcessingService;
        _openCvMatchService = openCvMatchService;
        _uiDumpParser = uiDumpParser;
        _codeGenerator = codeGenerator;
        _clipboardService = clipboardService;
        _messageService = messageService;
        _fileSaveDialogService = fileSaveDialogService;
        _filePickerService = filePickerService;

        _adbService.OperationLogged += OnOperationLogged;
        PropertyChanged += OnWorkflowPropertyChanged;
    }

    public event EventHandler<string>? CodePreviewRequested;
    public event EventHandler<global::Avalonia.Rect>? WidgetCanvasFocusRequested;

    [ObservableProperty]
    private WidgetTreeNodeViewModel? _selectedWidgetTreeNode;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _widgetTreeSummaryText = "显示 0 个业务节点";

    public bool CanCaptureScreenshot => SelectedDevice is not null && !IsBusy;

    public bool CanPullUiTree => SelectedDevice is not null && !IsBusy;

    public bool CanRunTemplateMatch =>
        HasCanvasContent &&
        !IsBusy &&
        (IsTemplateSourceBrowse ? _currentTemplateBytes is not null : CropRegion is not null && !CropRegion.IsEmpty);

    public bool CanExportTemplate => HasCanvasContent && CropRegion is not null && !CropRegion.IsEmpty && !IsBusy;

    public bool CanCopyOperationText => !string.IsNullOrWhiteSpace(OperationInputText) && OperationInputText != "[等待裁剪...]";

    public bool CanGenerateCode =>
        !IsBusy &&
        (IsImageMode
            ? (IsTemplateSourceBrowse ? !string.IsNullOrWhiteSpace(_currentTemplatePath) : !string.IsNullOrWhiteSpace(_lastExportedTemplatePath))
            : SelectedWidgetTreeNode is not null);

    public bool CanCopySelectedWidgetCoordinates => SelectedWidgetTreeNode is not null;

    public bool CanCopySelectedWidgetXPath => SelectedWidgetTreeNode is not null;

    public bool CanCopySelectedWidgetSelector => SelectedWidgetTreeNode is not null;

    private void OnWorkflowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CurrentMode):
            case nameof(IsWidgetBoundsVisible):
                OnPropertyChanged(nameof(CanGenerateCode));
                RefreshCanvasOverlays();
                break;
            case nameof(ShowTextWidgets):
            case nameof(ShowButtonWidgets):
            case nameof(ShowImageWidgets):
            case nameof(ShowOtherWidgets):
            case nameof(WidgetOverlayOpacity):
                RebuildWidgetOverlaysFromCurrentFilteredNodes();
                break;
            case nameof(SelectedDevice):
                OnPropertyChanged(nameof(CanCaptureScreenshot));
                OnPropertyChanged(nameof(CanPullUiTree));
                break;
            case nameof(CropRegion):
                _lastExportedTemplatePath = null;
                OnPropertyChanged(nameof(CanRunTemplateMatch));
                OnPropertyChanged(nameof(CanExportTemplate));
                OnPropertyChanged(nameof(CanGenerateCode));
                break;
            case nameof(IsTemplateSourceCurrent):
            case nameof(IsTemplateSourceBrowse):
            case nameof(SearchAllScreen):
                OnPropertyChanged(nameof(CanRunTemplateMatch));
                OnPropertyChanged(nameof(CanGenerateCode));
                ClearMatchCache();
                break;
            case nameof(IsBusy):
                OnPropertyChanged(nameof(CanCaptureScreenshot));
                OnPropertyChanged(nameof(CanPullUiTree));
                OnPropertyChanged(nameof(CanRunTemplateMatch));
                OnPropertyChanged(nameof(CanExportTemplate));
                OnPropertyChanged(nameof(CanGenerateCode));
                break;
            case nameof(MatchThreshold):
                ApplyMatchResultsFromCache();
                break;
            case nameof(SelectedWidgetTreeNode):
                UpdateSelectedWidgetState();
                break;
        }
    }
    [RelayCommand]
    private async Task RefreshDevicesAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var devices = await _adbService!.ScanDevicesAsync();
            Devices.Clear();
            foreach (var device in devices)
            {
                Devices.Add(new DeviceListItemViewModel(device.Serial, device.State, device.Model, device.ConnectionType));
            }

            if (SelectedDevice is not null)
            {
                SelectedDevice = Devices.FirstOrDefault(item => item.Serial == SelectedDevice.Serial);
            }
            else if (Devices.Count == 1)
            {
                SelectedDevice = Devices[0];
            }

            OnPropertyChanged(nameof(HasDevices));
            OnPropertyChanged(nameof(IsDeviceListEmpty));
            FooterStatusText = Devices.Count == 0 ? "未检测到设备" : $"已刷新设备列表：{Devices.Count} 台";

            if (Devices.Count == 0)
            {
                await ShowMessageAsync(UserMessageSeverity.Warning, "未检测到设备", "请确认 adb 已连接设备，并且设备处于在线状态。", CancellationToken.None);
            }
        }, "刷新设备列表失败");
    }

    [RelayCommand]
    private async Task CaptureScreenshotAsync()
    {
        if (SelectedDevice is null)
        {
            await ShowMessageAsync(UserMessageSeverity.Warning, "请先选择设备", "在多设备环境下，必须先选择目标设备后才能截屏。", CancellationToken.None);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var device = new AdbDevice
            {
                Serial = SelectedDevice.Serial,
                State = SelectedDevice.State,
                Model = SelectedDevice.Model,
                ConnectionType = SelectedDevice.ConnectionType
            };

            var screenshot = await _adbService!.CaptureScreenshotAsync(device);
            await SetScreenshotAsync(screenshot.PngData, null, true);
            FooterStatusText = $"截图完成：{screenshot.Width}x{screenshot.Height}";
        }, "截屏失败");
    }

    [RelayCommand]
    private async Task LoadScreenshotImageAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var filePath = await _filePickerService!.PickImageFileAsync("选择截图图片");
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            var bytes = await File.ReadAllBytesAsync(filePath);
            await SetScreenshotAsync(bytes, filePath, false);
            FooterStatusText = $"已载入图片：{Path.GetFileName(filePath)}";
        }, "载入图片失败");
    }

    [RelayCommand]
    private async Task BrowseTemplateImageAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            var filePath = await _filePickerService!.PickImageFileAsync("选择模板图片");
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            _currentTemplateBytes = await File.ReadAllBytesAsync(filePath);
            _currentTemplatePath = filePath;
            IsTemplateSourceBrowse = true;
            TemplateDirectoryText = filePath;
            TemplateName = Path.GetFileNameWithoutExtension(filePath);
            FooterStatusText = $"模板已载入：{Path.GetFileName(filePath)}";
            OnPropertyChanged(nameof(CanRunTemplateMatch));
            OnPropertyChanged(nameof(CanGenerateCode));
        }, "载入模板失败");
    }

    [RelayCommand]
    private async Task PullUiTreeAsync()
    {
        if (SelectedDevice is null)
        {
            await ShowMessageAsync(UserMessageSeverity.Warning, "请先选择设备", "拉取 UI 树之前必须先选择目标设备。", CancellationToken.None);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var device = new AdbDevice
            {
                Serial = SelectedDevice.Serial,
                State = SelectedDevice.State,
                Model = SelectedDevice.Model,
                ConnectionType = SelectedDevice.ConnectionType
            };

            var xml = await _adbService!.DumpUiHierarchyAsync(device);
            var root = await _uiDumpParser!.ParseAsync(xml);
            if (root is null)
            {
                throw new InvalidOperationException("UI dump 解析失败，未生成有效节点树。");
            }

            _widgetRoot = root;
            SeedExpandedNodes(root, maxDepthInclusive: 2);
            await RebuildFlatWidgetTreeAsync(WidgetTreeSearchText);
            CurrentMode = WorkbenchMode.Widget;
            FooterStatusText = WidgetTreeSummaryText;
        }, "拉取 UI 树失败");
    }

    [RelayCommand]
    private async Task RunTemplateMatchAsync()
    {
        if (_currentScreenshotBytes is null)
        {
            await ShowMessageAsync(UserMessageSeverity.Warning, "缺少截图", "请先截屏或载入截图图片。", CancellationToken.None);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var templateBytes = await ResolveTemplateBytesAsync();
            var searchRegion = SearchAllScreen ? null : CropRegion;
            var bestMatch = await _openCvMatchService!.MatchTemplateAsync(_currentScreenshotBytes, templateBytes, 0d, searchRegion);
            if (bestMatch is null)
            {
                throw new InvalidOperationException("模板匹配执行失败，未返回结果。");
            }

            _cachedBestMatch = bestMatch;
            _cachedMatchCandidates.Clear();
            var candidates = await _openCvMatchService.MatchTemplateMultiAsync(_currentScreenshotBytes, templateBytes, 0.5d, searchRegion);
            _cachedMatchCandidates.AddRange(candidates.OrderByDescending(item => item.Confidence));
            ApplyMatchResultsFromCache();
        }, "执行匹配失败");
    }

    [RelayCommand]
    private async Task ExportTemplateAsync()
    {
        if (_currentScreenshotBytes is null || CropRegion is null || CropRegion.IsEmpty)
        {
            await ShowMessageAsync(UserMessageSeverity.Warning, "缺少裁剪区域", "请先加载截图并创建有效裁剪区域。", CancellationToken.None);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var suggestedName = string.IsNullOrWhiteSpace(TemplateName) ? $"template-{DateTime.Now:yyyyMMdd-HHmmss}" : TemplateName.Trim();
            var saveResult = await _fileSaveDialogService!.SaveFileAsync(new SaveFileRequest(
                "导出模板 PNG",
                suggestedName + ".png",
                ".png",
                [new FileDialogFilter("PNG 图片", ["*.png"])]));

            if (!saveResult.Confirmed || string.IsNullOrWhiteSpace(saveResult.FilePath))
            {
                return;
            }

            var export = await _imageProcessingService!.ExportTemplateAsync(_currentScreenshotBytes, CropRegion, suggestedName);
            await File.WriteAllBytesAsync(saveResult.FilePath, export.TemplateBytes);
            var metadataPath = Path.ChangeExtension(saveResult.FilePath, ".json");
            await File.WriteAllTextAsync(metadataPath, export.MetadataJson, Encoding.UTF8);

            _currentTemplateBytes = export.TemplateBytes;
            _currentTemplatePath = saveResult.FilePath;
            _lastExportedTemplatePath = saveResult.FilePath;
            TemplateDirectoryText = saveResult.FilePath;
            TemplateName = Path.GetFileNameWithoutExtension(saveResult.FilePath);
            OperationInputText = $"已导出：{Path.GetFileName(saveResult.FilePath)}";
            FooterStatusText = $"模板已导出：{Path.GetFileName(saveResult.FilePath)}";
            OnPropertyChanged(nameof(CanRunTemplateMatch));
            OnPropertyChanged(nameof(CanGenerateCode));
        }, "导出模板失败");
    }

    [RelayCommand]
    private async Task CopyOperationTextAsync()
    {
        if (!CanCopyOperationText)
        {
            return;
        }

        await _clipboardService!.SetTextAsync(OperationInputText);
        FooterStatusText = "已复制当前操作文本";
    }

    [RelayCommand]
    private async Task CopySelectedWidgetCoordinatesAsync()
    {
        if (SelectedWidgetTreeNode is null)
        {
            return;
        }

        var rect = SelectedWidgetTreeNode.Node.BoundsRect;
        var text = $"[{rect.X}, {rect.Y}, {rect.Width}, {rect.Height}]";
        await _clipboardService!.SetTextAsync(text);
        FooterStatusText = "已复制控件坐标";
    }

    [RelayCommand]
    private async Task CopySelectedWidgetSelectorAsync()
    {
        if (SelectedWidgetTreeNode is null)
        {
            return;
        }

        var selector = _uiDumpParser!.GenerateUiSelector(SelectedWidgetTreeNode.Node);
        await _clipboardService!.SetTextAsync(selector);
        FooterStatusText = "已复制 UiSelector";
    }

    [RelayCommand]
    private async Task CopySelectedWidgetXPathAsync()
    {
        if (SelectedWidgetTreeNode is null || _widgetRoot is null)
        {
            return;
        }

        var xpath = BuildXPath(_widgetRoot, SelectedWidgetTreeNode.Node);
        if (string.IsNullOrWhiteSpace(xpath))
        {
            return;
        }

        await _clipboardService!.SetTextAsync(xpath);
        FooterStatusText = "已复制 XPath";
    }

    [RelayCommand]
    private async Task OpenCodePreviewAsync()
    {
        var code = await GenerateCodePreviewInternalAsync();
        if (!string.IsNullOrWhiteSpace(code))
        {
            _generatedCode = code;
            CodePreviewRequested?.Invoke(this, code);
        }
    }

    private async Task<byte[]> ResolveTemplateBytesAsync()
    {
        if (IsTemplateSourceBrowse)
        {
            return _currentTemplateBytes ?? throw new InvalidOperationException("请先浏览并载入模板图片。");
        }

        if (_currentScreenshotBytes is null || CropRegion is null || CropRegion.IsEmpty)
        {
            throw new InvalidOperationException("当前模板来源为裁剪区域，请先创建有效裁剪区域。");
        }

        return await _imageProcessingService!.CropAsync(_currentScreenshotBytes, CropRegion);
    }
    private async Task SetScreenshotAsync(byte[] screenshotBytes, string? sourcePath, bool fromDevice)
    {
        _currentScreenshotBytes = screenshotBytes;
        _currentScreenshotPath = sourcePath;
        IsScreenshotSourceCurrent = fromDevice;
        IsScreenshotSourceBrowse = !fromDevice;
        MatchResultSummary = "结果：等待执行匹配测试";
        ClearMatchCache(refreshSummary: false);

        await ReplaceCanvasBitmapAsync(screenshotBytes);
    }

    private async Task ReplaceCanvasBitmapAsync(byte[] imageBytes)
    {
        var newBitmap = CreateBitmap(imageBytes);
        var oldBitmap = CanvasBitmap;
        CanvasBitmap = newBitmap;
        oldBitmap?.Dispose();
        await Task.CompletedTask;
    }

    private static Bitmap CreateBitmap(byte[] imageBytes)
    {
        using var stream = new MemoryStream(imageBytes, writable: false);
        return new Bitmap(stream);
    }

    private async Task<string?> GenerateCodePreviewInternalAsync()
    {
        try
        {
            string code;
            if (IsImageMode)
            {
                var templatePath = IsTemplateSourceBrowse ? _currentTemplatePath : _lastExportedTemplatePath;
                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    await ShowMessageAsync(UserMessageSeverity.Warning, "请先准备模板", IsTemplateSourceBrowse ? "请先浏览并载入模板图片。" : "图像模式下生成代码前，请先导出模板 PNG。", CancellationToken.None);
                    return null;
                }

                code = _codeGenerator!.GenerateFullScript(new AutoJS6CodeOptions
                {
                    Mode = CodeGenerationMode.Image,
                    TemplatePath = templatePath,
                    Threshold = MatchThreshold,
                    Region = SearchAllScreen ? null : CropRegion,
                    GenerateLogging = true,
                    VariablePrefix = string.IsNullOrWhiteSpace(TemplateName) ? "target" : TemplateName
                });
            }
            else
            {
                if (SelectedWidgetTreeNode is null)
                {
                    await ShowMessageAsync(UserMessageSeverity.Warning, "请先选择控件", "控件模式下生成代码前，请先在节点树中选择一个控件。", CancellationToken.None);
                    return null;
                }

                code = _codeGenerator!.GenerateFullScript(new AutoJS6CodeOptions
                {
                    Mode = CodeGenerationMode.Widget,
                    Widget = SelectedWidgetTreeNode.Node,
                    GenerateLogging = true,
                    VariablePrefix = "widget"
                });
            }

            FooterStatusText = "代码预览已生成";
            return code;
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(UserMessageSeverity.Error, "生成代码失败", ex.Message, CancellationToken.None);
            return null;
        }
    }

    private void UpdateSelectedWidgetState()
    {
        if (SelectedWidgetTreeNode is null)
        {
            PropertyClassName = "-";
            PropertyText = "-";
            PropertyId = "-";
            PropertyBounds = "-";
            PropertyPackage = "-";
            PropertyClickable = "-";
            WidgetSelectionHint = "尚未选中控件。进入控件模式后，可在中央画布中点击控件边界框查看属性。";
        }
        else
        {
            var node = SelectedWidgetTreeNode.Node;
            PropertyClassName = string.IsNullOrWhiteSpace(node.ClassName) ? "-" : node.ClassName;
            PropertyText = string.IsNullOrWhiteSpace(node.Text) ? "-" : node.Text;
            PropertyId = string.IsNullOrWhiteSpace(node.ResourceId) ? "-" : node.ResourceId;
            PropertyBounds = $"[{node.BoundsRect.X}, {node.BoundsRect.Y}, {node.BoundsRect.Width}, {node.BoundsRect.Height}]";
            PropertyPackage = string.IsNullOrWhiteSpace(node.Package) ? "-" : node.Package;
            PropertyClickable = node.Clickable ? "true" : "false";
            WidgetSelectionHint = _uiDumpParser!.GenerateUiSelector(node);
            OperationInputText = $"[{node.BoundsRect.X}, {node.BoundsRect.Y}, {node.BoundsRect.Width}, {node.BoundsRect.Height}]";
        }

        OnPropertyChanged(nameof(CanGenerateCode));
        OnPropertyChanged(nameof(CanCopySelectedWidgetCoordinates));
        OnPropertyChanged(nameof(CanCopySelectedWidgetXPath));
        OnPropertyChanged(nameof(CanCopySelectedWidgetSelector));
        RefreshCanvasOverlays();

        if (CurrentMode == WorkbenchMode.Widget && SelectedWidgetTreeNode is not null)
        {
            var rect = SelectedWidgetTreeNode.Node.BoundsRect;
            WidgetCanvasFocusRequested?.Invoke(this, new global::Avalonia.Rect(rect.X, rect.Y, rect.Width, rect.Height));
        }
    }

    private async Task RebuildFlatWidgetTreeAsync(string searchText)
    {
        if (_widgetRoot is null || _uiDumpParser is null)
        {
            WidgetNodes.Clear();
            _widgetOverlays.Clear();
            WidgetTreeSummaryText = "显示 0 个业务节点";
            RefreshCanvasOverlays();
            OnPropertyChanged(nameof(HasWidgetNodes));
            OnPropertyChanged(nameof(IsWidgetTreeEmpty));
            return;
        }

        var result = await Task.Run(() => BuildFlatWidgetTreeResult(_widgetRoot, searchText));

        WidgetNodes.Clear();
        foreach (var item in result.FlatItems)
        {
            WidgetNodes.Add(item);
        }

        _currentFilteredWidgetNodes = result.FilteredBusinessNodes;
        RebuildWidgetOverlaysFromCurrentFilteredNodes();

        WidgetTreeSummaryText = $"显示 {result.FilteredBusinessNodes.Count} 个业务节点（原始 {result.TotalBusinessNodeCount}）";

        if (SelectedWidgetTreeNode is not null)
        {
            SelectedWidgetTreeNode = WidgetNodes.FirstOrDefault(item => ReferenceEquals(item.Node, SelectedWidgetTreeNode.Node));
        }

        OnPropertyChanged(nameof(HasWidgetNodes));
        OnPropertyChanged(nameof(IsWidgetTreeEmpty));
    }

    private void ApplyMatchResultsFromCache()
    {
        _matchOverlays.Clear();

        if (_cachedBestMatch is null)
        {
            MatchResultSummary = "结果：等待执行匹配测试";
            RefreshCanvasOverlays();
            return;
        }

        var filtered = _cachedMatchCandidates
            .Where(item => item.Confidence >= MatchThreshold)
            .OrderByDescending(item => item.Confidence)
            .ToArray();

        if (filtered.Length > 0)
        {
            foreach (var item in filtered)
            {
                _matchOverlays.Add(new CanvasOverlayRect(
                    new global::Avalonia.Rect(item.X, item.Y, item.Width, item.Height),
                    Color.Parse("#5CFF4A"),
                    2.2,
                    $"({item.ClickX}, {item.ClickY})  {item.Confidence:F3}",
                    Color.Parse("#57F857")));
            }

            MatchResultSummary = BuildMatchSuccessSummary(filtered[0], filtered.Length);
            FooterStatusText = filtered.Length == 1
                ? $"匹配成功：({filtered[0].ClickX}, {filtered[0].ClickY})"
                : $"匹配成功：{filtered.Length} 个候选结果";
        }
        else
        {
            MatchResultSummary = BuildMatchFailureSummary(_cachedBestMatch);
            FooterStatusText = "未找到高于阈值的匹配结果";
        }

        RefreshCanvasOverlays();
    }

    private void ClearMatchCache(bool refreshSummary = true)
    {
        _cachedBestMatch = null;
        _cachedMatchCandidates.Clear();
        _matchOverlays.Clear();

        if (refreshSummary)
        {
            MatchResultSummary = "结果：等待执行匹配测试";
        }

        RefreshCanvasOverlays();
    }

    [RelayCommand]
    private async Task ToggleWidgetNodeExpansionAsync(WidgetTreeNodeViewModel? node)
    {
        if (node is null || !node.HasChildren || _widgetRoot is null)
        {
            return;
        }

        node.IsExpanded = !node.IsExpanded;
        if (node.IsExpanded)
        {
            _expandedWidgetNodes.Add(node.Node);
        }
        else
        {
            _expandedWidgetNodes.Remove(node.Node);
        }

        await RebuildFlatWidgetTreeAsync(WidgetTreeSearchText);
    }

    private FlatWidgetTreeResult BuildFlatWidgetTreeResult(WidgetNode root, string searchText)
    {
        var hasFilter = !string.IsNullOrWhiteSpace(searchText);
        var allBusinessNodes = _uiDumpParser!.FilterNodes(root);
        var filteredBusinessNodes = hasFilter
            ? allBusinessNodes.Where(node => MatchesSearch(node, searchText)).ToArray()
            : allBusinessNodes;

        var flatItems = new List<WidgetTreeNodeViewModel>();
        foreach (var child in root.Children)
        {
            BuildFlatTreeRecursive(child, 0, searchText, hasFilter, flatItems);
        }

        return new FlatWidgetTreeResult(flatItems, filteredBusinessNodes, allBusinessNodes.Count);
    }

    private bool BuildFlatTreeRecursive(
        WidgetNode node,
        int depth,
        string searchText,
        bool hasFilter,
        ICollection<WidgetTreeNodeViewModel> output)
    {
        var childBuffer = new List<WidgetTreeNodeViewModel>();
        var descendantMatched = false;
        foreach (var child in node.Children)
        {
            if (BuildFlatTreeRecursive(child, depth + 1, searchText, hasFilter, childBuffer))
            {
                descendantMatched = true;
            }
        }

        var selfMatches = !hasFilter || MatchesSearch(node, searchText);
        var include = !hasFilter || selfMatches || descendantMatched;
        if (!include)
        {
            return false;
        }

        var viewModel = new WidgetTreeNodeViewModel(
            node,
            BuildNodeTitle(node),
            string.IsNullOrWhiteSpace(node.ResourceId) ? node.ClassName : node.ResourceId,
            depth)
        {
            IsExpanded = hasFilter ? (descendantMatched || selfMatches) : _expandedWidgetNodes.Contains(node)
        };

        output.Add(viewModel);

        if (!hasFilter)
        {
            if (viewModel.IsExpanded)
            {
                foreach (var child in childBuffer)
                {
                    output.Add(child);
                }
            }
        }
        else
        {
            foreach (var child in childBuffer)
            {
                output.Add(child);
            }
        }

        return true;
    }

    private void SeedExpandedNodes(WidgetNode node, int maxDepthInclusive)
    {
        if (node.Depth <= maxDepthInclusive)
        {
            _expandedWidgetNodes.Add(node);
        }

        foreach (var child in node.Children)
        {
            SeedExpandedNodes(child, maxDepthInclusive);
        }
    }

    private static string BuildNodeTitle(WidgetNode node)
    {
        if (!string.IsNullOrWhiteSpace(node.Text))
        {
            return $"{GetSimpleClassName(node.ClassName)} · {node.Text}";
        }

        if (!string.IsNullOrWhiteSpace(node.ContentDesc))
        {
            return $"{GetSimpleClassName(node.ClassName)} · {node.ContentDesc}";
        }

        return GetSimpleClassName(node.ClassName);
    }

    private static string BuildXPath(WidgetNode root, WidgetNode target)
    {
        var segments = new List<string>();
        return TryBuildXPathRecursive(null, root, target, segments)
            ? "/" + string.Join('/', segments)
            : string.Empty;
    }

    private static bool TryBuildXPathRecursive(WidgetNode? parent, WidgetNode current, WidgetNode target, IList<string> segments)
    {
        segments.Add(BuildXPathSegment(parent, current));
        if (ReferenceEquals(current, target))
        {
            return true;
        }

        foreach (var child in current.Children)
        {
            if (TryBuildXPathRecursive(current, child, target, segments))
            {
                return true;
            }
        }

        segments.RemoveAt(segments.Count - 1);
        return false;
    }

    private static string BuildXPathSegment(WidgetNode? parent, WidgetNode current)
    {
        var className = string.IsNullOrWhiteSpace(current.ClassName) ? "node" : current.ClassName;
        if (parent is null)
        {
            return className;
        }

        var siblingIndex = 1;
        foreach (var sibling in parent.Children)
        {
            if (ReferenceEquals(sibling, current))
            {
                break;
            }

            if (string.Equals(sibling.ClassName, current.ClassName, StringComparison.Ordinal))
            {
                siblingIndex++;
            }
        }

        return $"{className}[{siblingIndex}]";
    }

    private static string GetSimpleClassName(string? className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return "node";
        }

        var index = className.LastIndexOf('.');
        return index >= 0 ? className[(index + 1)..] : className;
    }

    private static bool MatchesSearch(WidgetNode node, string searchText)
    {
        return (node.ClassName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
               || (node.Text?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
               || (node.ContentDesc?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
               || (node.ResourceId?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static Color GetWidgetColor(WidgetNode node)
    {
        var className = node.ClassName ?? string.Empty;
        if (className.Contains("Text", StringComparison.OrdinalIgnoreCase))
        {
            return Color.Parse("#5A8CFF");
        }

        if (className.Contains("Button", StringComparison.OrdinalIgnoreCase) || node.Clickable)
        {
            return Color.Parse("#FFB84D");
        }

        if (className.Contains("Image", StringComparison.OrdinalIgnoreCase))
        {
            return Color.Parse("#4BD2A2");
        }

        return Color.Parse("#4C7DFF");
    }

    private void RebuildWidgetOverlaysFromCurrentFilteredNodes()
    {
        _widgetOverlays.Clear();

        foreach (var node in _currentFilteredWidgetNodes.Where(node => node.BoundsRect.Width > 0 && node.BoundsRect.Height > 0))
        {
            var category = GetWidgetCategory(node);
            if (!IsWidgetCategoryVisible(category))
            {
                continue;
            }

            var baseColor = GetWidgetColor(node);
            _widgetOverlays.Add(new CanvasOverlayRect(
                new global::Avalonia.Rect(node.BoundsRect.X, node.BoundsRect.Y, node.BoundsRect.Width, node.BoundsRect.Height),
                ApplyOpacity(baseColor, WidgetOverlayOpacity),
                1.6,
                null,
                null,
                ApplyOpacity(baseColor, Math.Clamp(WidgetOverlayOpacity * 0.22d, 0.06d, 0.35d))));
        }

        RefreshCanvasOverlays();
    }

    private WidgetVisualCategory GetWidgetCategory(WidgetNode node)
    {
        var className = node.ClassName ?? string.Empty;
        if (className.Contains("Text", StringComparison.OrdinalIgnoreCase))
        {
            return WidgetVisualCategory.Text;
        }

        if (className.Contains("Button", StringComparison.OrdinalIgnoreCase) || node.Clickable)
        {
            return WidgetVisualCategory.Button;
        }

        if (className.Contains("Image", StringComparison.OrdinalIgnoreCase))
        {
            return WidgetVisualCategory.Image;
        }

        return WidgetVisualCategory.Other;
    }

    private bool IsWidgetCategoryVisible(WidgetVisualCategory category)
    {
        return category switch
        {
            WidgetVisualCategory.Text => ShowTextWidgets,
            WidgetVisualCategory.Button => ShowButtonWidgets,
            WidgetVisualCategory.Image => ShowImageWidgets,
            _ => ShowOtherWidgets
        };
    }

    private static Color ApplyOpacity(Color color, double opacity)
    {
        var alpha = (byte)Math.Clamp(Math.Round(opacity * 255d), 0d, 255d);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    private void RefreshCanvasOverlays()
    {
        CanvasOverlays.Clear();

        if (IsWidgetMode)
        {
            foreach (var overlay in _widgetOverlays)
            {
                CanvasOverlays.Add(overlay);
            }

            if (SelectedWidgetTreeNode is not null)
            {
                var rect = SelectedWidgetTreeNode.Node.BoundsRect;
                CanvasOverlays.Add(new CanvasOverlayRect(new global::Avalonia.Rect(rect.X, rect.Y, rect.Width, rect.Height), Color.Parse("#FFB84D"), 2.4));
            }
        }
        else
        {
            foreach (var overlay in _matchOverlays)
            {
                CanvasOverlays.Add(overlay);
            }
        }
    }

    private static string BuildMatchSuccessSummary(MatchResult match, int matchCount)
    {
        return $"结果：命中\n匹配数量：{matchCount}\n最高置信度：{match.Confidence:F4}\n点击坐标：({match.ClickX}, {match.ClickY})\n匹配区域：({match.X}, {match.Y}, {match.Width}, {match.Height})\n耗时：{match.ElapsedMilliseconds} ms\n阈值：{match.Threshold:F2}";
    }

    private static string BuildMatchFailureSummary(MatchResult match)
    {
        return $"结果：未找到匹配\n最高置信度：{match.Confidence:F4}\n阈值：{match.Threshold:F2}\n候选区域：({match.X}, {match.Y}, {match.Width}, {match.Height})\n耗时：{match.ElapsedMilliseconds} ms";
    }
    private async Task ExecuteBusyAsync(Func<Task> action, string fallbackErrorTitle)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(UserMessageSeverity.Error, fallbackErrorTitle, ex.Message, CancellationToken.None);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ShowMessageAsync(UserMessageSeverity severity, string title, string message, CancellationToken cancellationToken)
    {
        if (_messageService is not null)
        {
            await _messageService.ShowAsync(new UserMessage(severity, title, message), cancellationToken);
        }
    }

    private void OnOperationLogged(object? sender, OperationLogEntry e)
    {
        AddLog(e.Category, e.ElapsedMilliseconds is null ? e.Message : $"{e.Message} ({e.ElapsedMilliseconds} ms)");
    }

    public async Task SelectWidgetNodeByCoordinateAsync(int pixelX, int pixelY)
    {
        if (_widgetRoot is null || _uiDumpParser is null)
        {
            return;
        }

        var node = _uiDumpParser.FindNodeByCoordinate(_widgetRoot, pixelX, pixelY);
        if (node is null)
        {
            return;
        }

        ExpandAncestors(_widgetRoot, node);
        await RebuildFlatWidgetTreeAsync(WidgetTreeSearchText);
        SelectedWidgetTreeNode = WidgetNodes.FirstOrDefault(item => ReferenceEquals(item.Node, node));
    }

    private bool ExpandAncestors(WidgetNode current, WidgetNode target)
    {
        if (ReferenceEquals(current, target))
        {
            return true;
        }

        foreach (var child in current.Children)
        {
            if (!ExpandAncestors(child, target))
            {
                continue;
            }

            _expandedWidgetNodes.Add(current);
            return true;
        }

        return false;
    }

    private sealed record FlatWidgetTreeResult(
        IReadOnlyList<WidgetTreeNodeViewModel> FlatItems,
        IReadOnlyList<WidgetNode> FilteredBusinessNodes,
        int TotalBusinessNodeCount);

    private enum WidgetVisualCategory
    {
        Text = 0,
        Button = 1,
        Image = 2,
        Other = 3
    }
}

