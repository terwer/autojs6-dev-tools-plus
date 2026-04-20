using App.Avalonia.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Core.Models;

namespace App.Avalonia.Controls;

public sealed class WorkbenchCanvasControl : Control
{
    public static readonly StyledProperty<Bitmap?> CanvasBitmapProperty =
        AvaloniaProperty.Register<WorkbenchCanvasControl, Bitmap?>(nameof(CanvasBitmap));

    public static readonly StyledProperty<IReadOnlyList<CanvasOverlayRect>?> OverlayRectsProperty =
        AvaloniaProperty.Register<WorkbenchCanvasControl, IReadOnlyList<CanvasOverlayRect>?>(nameof(OverlayRects));

    public static readonly StyledProperty<bool> ShowOverlayProperty =
        AvaloniaProperty.Register<WorkbenchCanvasControl, bool>(nameof(ShowOverlay), true);

    public static readonly StyledProperty<bool> IsCropModeEnabledProperty =
        AvaloniaProperty.Register<WorkbenchCanvasControl, bool>(nameof(IsCropModeEnabled));

    public static readonly StyledProperty<CropRegion?> CropRegionProperty =
        AvaloniaProperty.Register<WorkbenchCanvasControl, CropRegion?>(nameof(CropRegion), defaultBindingMode: BindingMode.TwoWay);

    private static readonly IBrush CanvasShellBrush = new SolidColorBrush(Color.Parse("#D3D4D7"));
    private static readonly IBrush StageBrush = new SolidColorBrush(Color.Parse("#3F434C"));
    private static readonly TimeSpan InertiaTickInterval = TimeSpan.FromMilliseconds(16);
    private readonly DispatcherTimer _inertiaTimer;
    private Vector _panOffset;
    private double _zoomFactor = 1d;
    private int _rotationDegrees;
    private bool _isDragging;
    private Point _lastPointerPosition;
    private DateTime _lastPointerMoveAtUtc = DateTime.UtcNow;
    private Vector _inertiaVelocity;
    private CropInteractionMode _cropInteractionMode = CropInteractionMode.None;
    private Rect _cropInteractionStartRect;
    private Point _cropInteractionStartPoint;
    private double _cropAspectRatio;
    private Point? _currentSourcePoint;
    private bool _isCrosshairVisible;

    static WorkbenchCanvasControl()
    {
        AffectsRender<WorkbenchCanvasControl>(CanvasBitmapProperty, OverlayRectsProperty, ShowOverlayProperty, IsCropModeEnabledProperty, CropRegionProperty);
    }

    public WorkbenchCanvasControl()
    {
        ClipToBounds = true;
        Focusable = true;
        _inertiaTimer = new DispatcherTimer(InertiaTickInterval, DispatcherPriority.Render, OnInertiaTick);
    }

    public event EventHandler<CanvasViewportChangedEventArgs>? ViewportChanged;
    public event EventHandler<CanvasPointerInfoChangedEventArgs>? PointerInfoChanged;

    public Bitmap? CanvasBitmap
    {
        get => GetValue(CanvasBitmapProperty);
        set => SetValue(CanvasBitmapProperty, value);
    }

    public IReadOnlyList<CanvasOverlayRect>? OverlayRects
    {
        get => GetValue(OverlayRectsProperty);
        set => SetValue(OverlayRectsProperty, value);
    }

    public bool ShowOverlay
    {
        get => GetValue(ShowOverlayProperty);
        set => SetValue(ShowOverlayProperty, value);
    }

    public bool IsCropModeEnabled
    {
        get => GetValue(IsCropModeEnabledProperty);
        set => SetValue(IsCropModeEnabledProperty, value);
    }

    public CropRegion? CropRegion
    {
        get => GetValue(CropRegionProperty);
        set => SetValue(CropRegionProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var controlRect = new Rect(Bounds.Size);
        if (controlRect.Width <= 0 || controlRect.Height <= 0)
        {
            return;
        }

        context.FillRectangle(CanvasShellBrush, controlRect, 22);

        var stageRect = GetStageRect(controlRect);
        context.FillRectangle(StageBrush, stageRect, 18);

        if (CanvasBitmap is null)
        {
            return;
        }

        var imageMatrix = BuildImageTransform(stageRect);
        var sourceRect = new Rect(0, 0, CanvasBitmap.PixelSize.Width, CanvasBitmap.PixelSize.Height);
        var destinationRect = new Rect(0, 0, CanvasBitmap.PixelSize.Width, CanvasBitmap.PixelSize.Height);

        using (context.PushTransform(imageMatrix))
        {
            RenderImageLayer(context, CanvasBitmap, sourceRect, destinationRect);

            if (ShowOverlay)
            {
                RenderOverlayLayer(context);
            }

            RenderCropLayer(context, stageRect);
            RenderPointerHelpers(context, stageRect);
        }
    }

    public void RotateClockwise90()
    {
        if (CanvasBitmap is null)
        {
            return;
        }

        _rotationDegrees = (_rotationDegrees + 90) % 360;
        _panOffset = ClampPanOffset(_panOffset, GetStageRect(new Rect(Bounds.Size)));
        StopInertia();
        RaiseViewportChanged();
        InvalidateVisual();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsCropModeEnabledProperty && !(change.NewValue as bool? ?? false))
        {
            _cropInteractionMode = CropInteractionMode.None;
        }

        if (change.Property == CropRegionProperty)
        {
            InvalidateVisual();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (CanvasBitmap is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        Focus();
        StopInertia();

        if (IsCropModeEnabled)
        {
            BeginCropInteraction(e);
            return;
        }

        _isDragging = true;
        _lastPointerPosition = e.GetPosition(this);
        _lastPointerMoveAtUtc = DateTime.UtcNow;
        _inertiaVelocity = default;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (CanvasBitmap is null)
        {
            ClearPointerInfo();
            return;
        }

        UpdatePointerInfo(e.GetPosition(this), e.KeyModifiers);

        if (IsCropModeEnabled && _cropInteractionMode != CropInteractionMode.None)
        {
            UpdateCropInteraction(e);
            return;
        }

        if (!_isDragging)
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        var delta = currentPosition - _lastPointerPosition;
        var now = DateTime.UtcNow;
        var elapsed = Math.Max((now - _lastPointerMoveAtUtc).TotalSeconds, 0.001d);

        _panOffset = ClampPanOffset(_panOffset + delta, GetStageRect(new Rect(Bounds.Size)));
        _inertiaVelocity = delta / elapsed;
        _lastPointerMoveAtUtc = now;
        _lastPointerPosition = currentPosition;

        RaiseViewportChanged();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (IsCropModeEnabled && _cropInteractionMode != CropInteractionMode.None)
        {
            _cropInteractionMode = CropInteractionMode.None;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        e.Pointer.Capture(null);
        StartInertiaIfNeeded();
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isDragging = false;
        _cropInteractionMode = CropInteractionMode.None;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        ClearPointerInfo();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (CanvasBitmap is null)
        {
            return;
        }

        var stageRect = GetStageRect(new Rect(Bounds.Size));
        var pointerPosition = e.GetPosition(this);
        if (!stageRect.Contains(pointerPosition))
        {
            return;
        }

        var factor = e.Delta.Y > 0 ? 1.12d : 1d / 1.12d;
        var targetZoom = Math.Clamp(_zoomFactor * factor, 0.1d, 5d);
        if (Math.Abs(targetZoom - _zoomFactor) < 0.0001d)
        {
            return;
        }

        var sourcePoint = TryMapViewToSource(pointerPosition, stageRect, out var mappedSource)
            ? mappedSource
            : new Point(CanvasBitmap.PixelSize.Width / 2d, CanvasBitmap.PixelSize.Height / 2d);

        _zoomFactor = targetZoom;
        _panOffset = CalculatePanOffsetForSourcePoint(stageRect, sourcePoint, pointerPosition);
        _panOffset = ClampPanOffset(_panOffset, stageRect);
        StopInertia();

        RaiseViewportChanged();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
        {
            _isCrosshairVisible = _currentSourcePoint.HasValue;
            RaisePointerInfoChanged();
            InvalidateVisual();
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
        {
            _isCrosshairVisible = false;
            RaisePointerInfoChanged();
            InvalidateVisual();
        }
    }

    private void BeginCropInteraction(PointerPressedEventArgs e)
    {
        var stageRect = GetStageRect(new Rect(Bounds.Size));
        if (!TryMapViewToSource(e.GetPosition(this), stageRect, out var sourcePoint))
        {
            return;
        }

        sourcePoint = ClampSourcePoint(sourcePoint);
        _lastPointerPosition = e.GetPosition(this);
        _lastPointerMoveAtUtc = DateTime.UtcNow;
        e.Pointer.Capture(this);

        var handle = HitTestCropHandle(sourcePoint, GetCropHandleHitTolerance(stageRect));
        if (handle == CropInteractionMode.None)
        {
            _cropInteractionMode = CropInteractionMode.Create;
            _cropInteractionStartPoint = sourcePoint;
            _cropAspectRatio = 0d;
            SetCropRegionFromRect(new Rect(sourcePoint, sourcePoint));
        }
        else
        {
            _cropInteractionMode = handle;
            _cropInteractionStartRect = CropRegionToRect(CropRegion!);
            _cropInteractionStartPoint = sourcePoint;
            _cropAspectRatio = _cropInteractionStartRect.Height <= 0d ? 1d : _cropInteractionStartRect.Width / _cropInteractionStartRect.Height;
        }

        e.Handled = true;
    }

    private void UpdateCropInteraction(PointerEventArgs e)
    {
        var stageRect = GetStageRect(new Rect(Bounds.Size));
        if (!TryMapViewToSource(e.GetPosition(this), stageRect, out var sourcePoint))
        {
            return;
        }

        sourcePoint = ClampSourcePoint(sourcePoint);
        var lockAspectRatio = e.KeyModifiers.HasFlag(KeyModifiers.Shift) && _cropAspectRatio > 0d;

        Rect cropRect;
        if (_cropInteractionMode == CropInteractionMode.Create)
        {
            cropRect = CreateNormalizedRect(_cropInteractionStartPoint, sourcePoint);
        }
        else
        {
            cropRect = ResizeCropRect(_cropInteractionStartRect, _cropInteractionMode, sourcePoint, lockAspectRatio, _cropAspectRatio);
        }

        cropRect = ClampCropRectToImage(cropRect);
        SetCropRegionFromRect(cropRect);
        e.Handled = true;
    }

    private static Rect GetStageRect(Rect controlRect)
    {
        return controlRect.Deflate(14);
    }

    private Matrix BuildImageTransform(Rect stageRect)
    {
        if (CanvasBitmap is null)
        {
            return Matrix.Identity;
        }

        var scale = CalculateBaseScale(stageRect);
        var center = stageRect.Center + _panOffset;
        var bitmapCenter = new Point(CanvasBitmap.PixelSize.Width / 2d, CanvasBitmap.PixelSize.Height / 2d);

        return Matrix.Identity
            .Append(Matrix.CreateTranslation(-bitmapCenter.X, -bitmapCenter.Y))
            .Append(Matrix.CreateScale(scale * _zoomFactor, scale * _zoomFactor))
            .Append(Matrix.CreateRotation(Matrix.ToRadians(_rotationDegrees)))
            .Append(Matrix.CreateTranslation(center.X, center.Y));
    }

    private double CalculateBaseScale(Rect stageRect)
    {
        if (CanvasBitmap is null)
        {
            return 1d;
        }

        var sourceWidth = (double)CanvasBitmap.PixelSize.Width;
        var sourceHeight = (double)CanvasBitmap.PixelSize.Height;
        var rotatedWidth = _rotationDegrees % 180 == 0 ? sourceWidth : sourceHeight;
        var rotatedHeight = _rotationDegrees % 180 == 0 ? sourceHeight : sourceWidth;

        return Math.Min(stageRect.Width / rotatedWidth, stageRect.Height / rotatedHeight);
    }

    private Size CalculateTransformedContentSize(Rect stageRect)
    {
        if (CanvasBitmap is null)
        {
            return default;
        }

        var baseScale = CalculateBaseScale(stageRect) * _zoomFactor;
        var rotatedWidth = _rotationDegrees % 180 == 0 ? CanvasBitmap.PixelSize.Width : CanvasBitmap.PixelSize.Height;
        var rotatedHeight = _rotationDegrees % 180 == 0 ? CanvasBitmap.PixelSize.Height : CanvasBitmap.PixelSize.Width;
        return new Size(rotatedWidth * baseScale, rotatedHeight * baseScale);
    }

    private Vector ClampPanOffset(Vector value, Rect stageRect)
    {
        var contentSize = CalculateTransformedContentSize(stageRect);
        var maxOffsetX = Math.Max(0d, (contentSize.Width - stageRect.Width) / 2d);
        var maxOffsetY = Math.Max(0d, (contentSize.Height - stageRect.Height) / 2d);

        return new Vector(
            Math.Clamp(value.X, -maxOffsetX, maxOffsetX),
            Math.Clamp(value.Y, -maxOffsetY, maxOffsetY));
    }

    private bool TryMapViewToSource(Point viewPoint, Rect stageRect, out Point sourcePoint)
    {
        var matrix = BuildImageTransform(stageRect);
        if (!matrix.TryInvert(out var inverse))
        {
            sourcePoint = default;
            return false;
        }

        sourcePoint = inverse.Transform(viewPoint);
        return true;
    }

    private Vector CalculatePanOffsetForSourcePoint(Rect stageRect, Point sourcePoint, Point desiredViewPoint)
    {
        if (CanvasBitmap is null)
        {
            return default;
        }

        var savedPan = _panOffset;
        _panOffset = default;
        var baseTransform = BuildImageTransform(stageRect);
        _panOffset = savedPan;

        var transformedPoint = baseTransform.Transform(sourcePoint);
        return desiredViewPoint - transformedPoint;
    }

    private double GetCropHandleHitTolerance(Rect stageRect)
    {
        var currentScale = Math.Max(CalculateBaseScale(stageRect) * _zoomFactor, 0.01d);
        return 10d / currentScale;
    }

    private Point ClampSourcePoint(Point point)
    {
        if (CanvasBitmap is null)
        {
            return point;
        }

        return new Point(
            Math.Clamp(point.X, 0d, CanvasBitmap.PixelSize.Width),
            Math.Clamp(point.Y, 0d, CanvasBitmap.PixelSize.Height));
    }

    private CropInteractionMode HitTestCropHandle(Point sourcePoint, double tolerance)
    {
        if (CropRegion is null || CropRegion.IsEmpty)
        {
            return CropInteractionMode.None;
        }

        var rect = CropRegionToRect(CropRegion);
        var left = rect.Left;
        var right = rect.Right;
        var top = rect.Top;
        var bottom = rect.Bottom;
        var centerX = rect.Center.X;
        var centerY = rect.Center.Y;

        if (IsNear(sourcePoint, new Point(left, top), tolerance)) return CropInteractionMode.TopLeft;
        if (IsNear(sourcePoint, new Point(right, top), tolerance)) return CropInteractionMode.TopRight;
        if (IsNear(sourcePoint, new Point(left, bottom), tolerance)) return CropInteractionMode.BottomLeft;
        if (IsNear(sourcePoint, new Point(right, bottom), tolerance)) return CropInteractionMode.BottomRight;

        if (Math.Abs(sourcePoint.Y - top) <= tolerance && sourcePoint.X >= left && sourcePoint.X <= right) return CropInteractionMode.Top;
        if (Math.Abs(sourcePoint.Y - bottom) <= tolerance && sourcePoint.X >= left && sourcePoint.X <= right) return CropInteractionMode.Bottom;
        if (Math.Abs(sourcePoint.X - left) <= tolerance && sourcePoint.Y >= top && sourcePoint.Y <= bottom) return CropInteractionMode.Left;
        if (Math.Abs(sourcePoint.X - right) <= tolerance && sourcePoint.Y >= top && sourcePoint.Y <= bottom) return CropInteractionMode.Right;

        return CropInteractionMode.None;
    }

    private static bool IsNear(Point a, Point b, double tolerance)
    {
        return Math.Abs(a.X - b.X) <= tolerance && Math.Abs(a.Y - b.Y) <= tolerance;
    }

    private Rect ResizeCropRect(Rect initialRect, CropInteractionMode mode, Point currentPoint, bool lockAspectRatio, double aspectRatio)
    {
        var left = initialRect.Left;
        var top = initialRect.Top;
        var right = initialRect.Right;
        var bottom = initialRect.Bottom;

        switch (mode)
        {
            case CropInteractionMode.Left:
            case CropInteractionMode.TopLeft:
            case CropInteractionMode.BottomLeft:
                left = currentPoint.X;
                break;
            case CropInteractionMode.Right:
            case CropInteractionMode.TopRight:
            case CropInteractionMode.BottomRight:
                right = currentPoint.X;
                break;
        }

        switch (mode)
        {
            case CropInteractionMode.Top:
            case CropInteractionMode.TopLeft:
            case CropInteractionMode.TopRight:
                top = currentPoint.Y;
                break;
            case CropInteractionMode.Bottom:
            case CropInteractionMode.BottomLeft:
            case CropInteractionMode.BottomRight:
                bottom = currentPoint.Y;
                break;
        }

        var result = NormalizeRect(new Rect(left, top, right - left, bottom - top));

        if (!lockAspectRatio || mode is CropInteractionMode.Left or CropInteractionMode.Right or CropInteractionMode.Top or CropInteractionMode.Bottom)
        {
            return result;
        }

        return ApplyAspectRatio(result, mode, aspectRatio);
    }

    private static Rect ApplyAspectRatio(Rect rect, CropInteractionMode mode, double aspectRatio)
    {
        if (aspectRatio <= 0d || rect.Width <= 0d || rect.Height <= 0d)
        {
            return rect;
        }

        var widthBasedHeight = rect.Width / aspectRatio;
        var heightBasedWidth = rect.Height * aspectRatio;
        var useWidth = widthBasedHeight <= rect.Height;

        var targetWidth = useWidth ? rect.Width : heightBasedWidth;
        var targetHeight = useWidth ? widthBasedHeight : rect.Height;

        return mode switch
        {
            CropInteractionMode.TopLeft => new Rect(rect.Right - targetWidth, rect.Bottom - targetHeight, targetWidth, targetHeight),
            CropInteractionMode.TopRight => new Rect(rect.Left, rect.Bottom - targetHeight, targetWidth, targetHeight),
            CropInteractionMode.BottomLeft => new Rect(rect.Right - targetWidth, rect.Top, targetWidth, targetHeight),
            CropInteractionMode.BottomRight => new Rect(rect.Left, rect.Top, targetWidth, targetHeight),
            _ => rect
        };
    }

    private Rect ClampCropRectToImage(Rect rect)
    {
        if (CanvasBitmap is null)
        {
            return rect;
        }

        var left = Math.Clamp(rect.Left, 0d, CanvasBitmap.PixelSize.Width);
        var top = Math.Clamp(rect.Top, 0d, CanvasBitmap.PixelSize.Height);
        var right = Math.Clamp(rect.Right, 0d, CanvasBitmap.PixelSize.Width);
        var bottom = Math.Clamp(rect.Bottom, 0d, CanvasBitmap.PixelSize.Height);
        return NormalizeRect(new Rect(left, top, right - left, bottom - top));
    }

    private void SetCropRegionFromRect(Rect rect)
    {
        rect = NormalizeRect(rect);
        if (rect.Width < 1d || rect.Height < 1d)
        {
            CropRegion = null;
            return;
        }

        CropRegion = new CropRegion
        {
            X = (int)Math.Round(rect.X),
            Y = (int)Math.Round(rect.Y),
            Width = Math.Max(1, (int)Math.Round(rect.Width)),
            Height = Math.Max(1, (int)Math.Round(rect.Height)),
            OriginalWidth = CanvasBitmap?.PixelSize.Width,
            OriginalHeight = CanvasBitmap?.PixelSize.Height,
            ReferenceWidth = CanvasBitmap?.PixelSize.Width,
            ReferenceHeight = CanvasBitmap?.PixelSize.Height
        };
    }

    private static Rect CropRegionToRect(CropRegion cropRegion)
    {
        return new Rect(cropRegion.X, cropRegion.Y, cropRegion.Width, cropRegion.Height);
    }

    private static Rect CreateNormalizedRect(Point start, Point end)
    {
        return NormalizeRect(new Rect(start, end));
    }

    private static Rect NormalizeRect(Rect rect)
    {
        var left = Math.Min(rect.Left, rect.Right);
        var top = Math.Min(rect.Top, rect.Bottom);
        var right = Math.Max(rect.Left, rect.Right);
        var bottom = Math.Max(rect.Top, rect.Bottom);
        return new Rect(left, top, right - left, bottom - top);
    }

    private static void RenderImageLayer(DrawingContext context, Bitmap bitmap, Rect sourceRect, Rect destinationRect)
    {
        context.DrawImage(bitmap, sourceRect, destinationRect);
    }

    private void RenderOverlayLayer(DrawingContext context)
    {
        if (OverlayRects is null || OverlayRects.Count == 0)
        {
            return;
        }

        foreach (var overlay in OverlayRects)
        {
            var pen = new Pen(new SolidColorBrush(overlay.StrokeColor), overlay.StrokeThickness);
            context.DrawRectangle(null, pen, overlay.PixelRect);
        }
    }

    private void RenderCropLayer(DrawingContext context, Rect stageRect)
    {
        if (CropRegion is null || CropRegion.IsEmpty)
        {
            return;
        }

        var rect = CropRegionToRect(CropRegion);
        var currentScale = Math.Max(CalculateBaseScale(stageRect) * _zoomFactor, 0.01d);
        var strokeThickness = 2d / currentScale;
        var handleSize = 10d / currentScale;
        var pen = new Pen(new SolidColorBrush(Color.Parse("#F5A623")), strokeThickness);
        var fill = new SolidColorBrush(Color.Parse(IsCropModeEnabled ? "#1AF5A623" : "#10F5A623"));

        context.DrawRectangle(fill, pen, rect);

        var handleBrush = new SolidColorBrush(Color.Parse("#FFF7D24D"));
        foreach (var handleRect in GetHandleRects(rect, handleSize))
        {
            context.DrawRectangle(handleBrush, pen, handleRect);
        }
    }

    private void RenderPointerHelpers(DrawingContext context, Rect stageRect)
    {
        if (_currentSourcePoint is not { } sourcePoint || CanvasBitmap is null)
        {
            return;
        }

        var currentScale = Math.Max(CalculateBaseScale(stageRect) * _zoomFactor, 0.01d);
        var pointerMarkerSize = 8d / currentScale;
        var markerPen = new Pen(new SolidColorBrush(Color.Parse("#4AFF4A")), 2d / currentScale);
        context.DrawEllipse(null, markerPen, sourcePoint, pointerMarkerSize / 2d, pointerMarkerSize / 2d);

        if (!_isCrosshairVisible)
        {
            return;
        }

        var gridPen = new Pen(new SolidColorBrush(Color.Parse("#2AFFFFFF")), 1d / currentScale);
        const int gridStep = 100;
        for (var x = gridStep; x < CanvasBitmap.PixelSize.Width; x += gridStep)
        {
            context.DrawLine(gridPen, new Point(x, 0), new Point(x, CanvasBitmap.PixelSize.Height));
        }

        for (var y = gridStep; y < CanvasBitmap.PixelSize.Height; y += gridStep)
        {
            context.DrawLine(gridPen, new Point(0, y), new Point(CanvasBitmap.PixelSize.Width, y));
        }

        var crosshairPen = new Pen(new SolidColorBrush(Color.Parse("#7CFF4A")), 1.5d / currentScale);
        context.DrawLine(crosshairPen, new Point(sourcePoint.X, 0), new Point(sourcePoint.X, CanvasBitmap.PixelSize.Height));
        context.DrawLine(crosshairPen, new Point(0, sourcePoint.Y), new Point(CanvasBitmap.PixelSize.Width, sourcePoint.Y));
    }

    private static IEnumerable<Rect> GetHandleRects(Rect rect, double size)
    {
        var half = size / 2d;
        var points = new[]
        {
            new Point(rect.Left, rect.Top),
            new Point(rect.Center.X, rect.Top),
            new Point(rect.Right, rect.Top),
            new Point(rect.Left, rect.Center.Y),
            new Point(rect.Right, rect.Center.Y),
            new Point(rect.Left, rect.Bottom),
            new Point(rect.Center.X, rect.Bottom),
            new Point(rect.Right, rect.Bottom)
        };

        foreach (var point in points)
        {
            yield return new Rect(point.X - half, point.Y - half, size, size);
        }
    }

    private void StartInertiaIfNeeded()
    {
        if (_inertiaVelocity.Length < 700d)
        {
            _inertiaVelocity = default;
            return;
        }

        _inertiaTimer.Start();
    }

    private void StopInertia()
    {
        _inertiaTimer.Stop();
        _inertiaVelocity = default;
    }

    private void OnInertiaTick(object? sender, EventArgs e)
    {
        if (CanvasBitmap is null)
        {
            StopInertia();
            return;
        }

        var dt = InertiaTickInterval.TotalSeconds;
        var nextOffset = ClampPanOffset(_panOffset + _inertiaVelocity * dt, GetStageRect(new Rect(Bounds.Size)));
        var expectedOffsetX = _panOffset.X + _inertiaVelocity.X * dt;
        var expectedOffsetY = _panOffset.Y + _inertiaVelocity.Y * dt;
        var wasClampedX = Math.Abs(nextOffset.X - expectedOffsetX) > 0.01d;
        var wasClampedY = Math.Abs(nextOffset.Y - expectedOffsetY) > 0.01d;

        _panOffset = nextOffset;
        _inertiaVelocity *= 0.88d;

        if (wasClampedX)
        {
            _inertiaVelocity = new Vector(0d, _inertiaVelocity.Y);
        }

        if (wasClampedY)
        {
            _inertiaVelocity = new Vector(_inertiaVelocity.X, 0d);
        }

        RaiseViewportChanged();
        InvalidateVisual();

        if (_inertiaVelocity.Length < 15d)
        {
            StopInertia();
        }
    }

    private void RaiseViewportChanged()
    {
        ViewportChanged?.Invoke(this, new CanvasViewportChangedEventArgs
        {
            ZoomFactor = _zoomFactor,
            PanOffset = _panOffset,
            RotationDegrees = _rotationDegrees
        });
    }

    private void UpdatePointerInfo(Point viewPoint, KeyModifiers keyModifiers)
    {
        var stageRect = GetStageRect(new Rect(Bounds.Size));
        if (CanvasBitmap is null || !stageRect.Contains(viewPoint) || !TryMapViewToSource(viewPoint, stageRect, out var sourcePoint))
        {
            ClearPointerInfo();
            return;
        }

        sourcePoint = ClampSourcePoint(sourcePoint);
        _currentSourcePoint = sourcePoint;
        _isCrosshairVisible = keyModifiers.HasFlag(KeyModifiers.Control);
        RaisePointerInfoChanged();
        InvalidateVisual();
    }

    private void ClearPointerInfo()
    {
        if (_currentSourcePoint is null && !_isCrosshairVisible)
        {
            return;
        }

        _currentSourcePoint = null;
        _isCrosshairVisible = false;
        RaisePointerInfoChanged();
        InvalidateVisual();
    }

    private void RaisePointerInfoChanged()
    {
        PointerInfoChanged?.Invoke(this, new CanvasPointerInfoChangedEventArgs
        {
            PixelX = _currentSourcePoint is null ? null : (int)Math.Round(_currentSourcePoint.Value.X),
            PixelY = _currentSourcePoint is null ? null : (int)Math.Round(_currentSourcePoint.Value.Y),
            IsCrosshairVisible = _isCrosshairVisible
        });
    }

    private enum CropInteractionMode
    {
        None = 0,
        Create = 1,
        Left = 2,
        Top = 3,
        Right = 4,
        Bottom = 5,
        TopLeft = 6,
        TopRight = 7,
        BottomLeft = 8,
        BottomRight = 9
    }
}
