namespace Core.Models;

/// <summary>
/// 经过处理后的图像结果。
/// </summary>
public sealed record ProcessedImage(
    byte[] EncodedBytes,
    ImageInfo Original,
    ImageInfo Current,
    bool WasDownsampled,
    double ScaleFactor);
