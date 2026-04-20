using Avalonia;
using Core.Models;

namespace App.Avalonia.Models;

public sealed class CanvasViewportChangedEventArgs : EventArgs
{
    public required double ZoomFactor { get; init; }

    public required Vector PanOffset { get; init; }

    public required int RotationDegrees { get; init; }
}

public sealed class CanvasCropRegionChangedEventArgs : EventArgs
{
    public CropRegion? CropRegion { get; init; }
}

public sealed class CanvasPointerInfoChangedEventArgs : EventArgs
{
    public int? PixelX { get; init; }

    public int? PixelY { get; init; }

    public bool IsCrosshairVisible { get; init; }
}

public sealed class CanvasOverlayClickEventArgs : EventArgs
{
    public required int PixelX { get; init; }

    public required int PixelY { get; init; }
}
