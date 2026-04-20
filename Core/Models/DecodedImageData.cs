namespace Core.Models;

/// <summary>
/// 解码后的位图像素数据。
/// </summary>
public sealed record DecodedImageData(byte[] PixelData, int Width, int Height, string PixelFormat);
