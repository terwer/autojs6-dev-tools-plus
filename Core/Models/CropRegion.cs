namespace Core.Models;

/// <summary>
/// 裁剪区域信息，采用左上角原点坐标系。
/// </summary>
public sealed class CropRegion
{
    /// <summary>
    /// 左上角 X 坐标。
    /// </summary>
    public required int X { get; init; }

    /// <summary>
    /// 左上角 Y 坐标。
    /// </summary>
    public required int Y { get; init; }

    /// <summary>
    /// 区域宽度。
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// 区域高度。
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// 可选名称。
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// 原图宽度。
    /// </summary>
    public int? OriginalWidth { get; init; }

    /// <summary>
    /// 原图高度。
    /// </summary>
    public int? OriginalHeight { get; init; }

    /// <summary>
    /// 参考分辨率宽度。
    /// </summary>
    public int? ReferenceWidth { get; init; }

    /// <summary>
    /// 参考分辨率高度。
    /// </summary>
    public int? ReferenceHeight { get; init; }

    public int Right => X + Width;

    public int Bottom => Y + Height;

    public int Area => Width * Height;

    public bool IsEmpty => Width <= 0 || Height <= 0;
}
