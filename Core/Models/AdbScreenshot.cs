namespace Core.Models;

/// <summary>
/// ADB 拉取的截图结果。
/// </summary>
public sealed record AdbScreenshot(byte[] PngData, int Width, int Height);
