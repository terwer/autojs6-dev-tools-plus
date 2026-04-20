using Avalonia;
using Avalonia.Media;

namespace App.Avalonia.Models;

public sealed record CanvasOverlayRect(
    Rect PixelRect,
    Color StrokeColor,
    double StrokeThickness = 1.5,
    string? Label = null,
    Color? LabelColor = null,
    Color? FillColor = null);
