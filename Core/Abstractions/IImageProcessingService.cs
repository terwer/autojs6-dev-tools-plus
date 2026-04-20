using Core.Models;

namespace Core.Abstractions;

/// <summary>
/// 跨平台图像处理服务抽象。
/// </summary>
public interface IImageProcessingService
{
    Task<DecodedImageData> DecodeAsync(byte[] imageData, CancellationToken cancellationToken = default);

    Task<ProcessedImage> DownsampleAsync(
        byte[] imageData,
        int maxWidth = 1920,
        int maxHeight = 1080,
        CancellationToken cancellationToken = default);

    Task<byte[]> CropAsync(byte[] imageData, CropRegion region, CancellationToken cancellationToken = default);

    Task<TemplateExportResult> ExportTemplateAsync(
        byte[] imageData,
        CropRegion region,
        string? templateName = null,
        CancellationToken cancellationToken = default);

    Task<ImageInfo> GetImageInfoAsync(byte[] imageData, CancellationToken cancellationToken = default);

    Task<bool> ValidateImageAsync(byte[] imageData, CancellationToken cancellationToken = default);
}
