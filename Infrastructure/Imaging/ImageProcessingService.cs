using System.Text.Json;
using Core.Abstractions;
using Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageInfoModel = Core.Models.ImageInfo;

namespace Infrastructure.Imaging;

/// <summary>
/// 基于 ImageSharp 的跨平台图像处理实现。
/// </summary>
public sealed class ImageProcessingService : IImageProcessingService
{
    public async Task<DecodedImageData> DecodeAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageData);

        using var image = await LoadImageAsync(imageData, cancellationToken);
        var pixelData = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixelData);

        return new DecodedImageData(pixelData, image.Width, image.Height, "RGBA8888");
    }

    public async Task<ProcessedImage> DownsampleAsync(
        byte[] imageData,
        int maxWidth = 1920,
        int maxHeight = 1080,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageData);

        if (maxWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxWidth));
        }

        if (maxHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHeight));
        }

        using var image = await LoadImageAsync(imageData, cancellationToken);
        var original = new ImageInfoModel(image.Width, image.Height, "image/png");
        var scaleFactor = Math.Min(1d, Math.Min(maxWidth / (double)image.Width, maxHeight / (double)image.Height));
        var wasDownsampled = scaleFactor < 1d;

        if (wasDownsampled)
        {
            var targetWidth = Math.Max(1, (int)Math.Floor(image.Width * scaleFactor));
            var targetHeight = Math.Max(1, (int)Math.Floor(image.Height * scaleFactor));
            image.Mutate(context => context.Resize(targetWidth, targetHeight));
        }

        var encodedBytes = await EncodeAsPngAsync(image, cancellationToken);
        var current = new ImageInfoModel(image.Width, image.Height, "image/png");

        return new ProcessedImage(encodedBytes, original, current, wasDownsampled, scaleFactor);
    }

    public async Task<byte[]> CropAsync(byte[] imageData, CropRegion region, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageData);
        ArgumentNullException.ThrowIfNull(region);
        ValidateCropRegion(region);

        using var image = await LoadImageAsync(imageData, cancellationToken);
        EnsureCropRegionInsideImage(region, image.Width, image.Height);
        image.Mutate(context => context.Crop(new Rectangle(region.X, region.Y, region.Width, region.Height)));

        return await EncodeAsPngAsync(image, cancellationToken);
    }

    public async Task<TemplateExportResult> ExportTemplateAsync(
        byte[] imageData,
        CropRegion region,
        string? templateName = null,
        CancellationToken cancellationToken = default)
    {
        var originalInfo = await GetImageInfoAsync(imageData, cancellationToken);
        var templateBytes = await CropAsync(imageData, region, cancellationToken);
        var templateInfo = await GetImageInfoAsync(templateBytes, cancellationToken);

        var metadata = new
        {
            templateName,
            originalWidth = originalInfo.Width,
            originalHeight = originalInfo.Height,
            crop = new
            {
                x = region.X,
                y = region.Y,
                width = region.Width,
                height = region.Height
            },
            exportedAtUtc = DateTimeOffset.UtcNow
        };

        var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return new TemplateExportResult(templateBytes, metadataJson, region, originalInfo, templateInfo);
    }

    public async Task<ImageInfoModel> GetImageInfoAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imageData);

        using var image = await LoadImageAsync(imageData, cancellationToken);
        return new ImageInfoModel(image.Width, image.Height, "image/png");
    }

    public async Task<bool> ValidateImageAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await GetImageInfoAsync(imageData, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<Image<Rgba32>> LoadImageAsync(byte[] imageData, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(imageData, writable: false);
        return await Image.LoadAsync<Rgba32>(stream, cancellationToken);
    }

    private static async Task<byte[]> EncodeAsPngAsync(Image<Rgba32> image, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder(), cancellationToken);
        return stream.ToArray();
    }

    private static void ValidateCropRegion(CropRegion region)
    {
        if (region.IsEmpty)
        {
            throw new ArgumentException("裁剪区域必须是有效的非空矩形。", nameof(region));
        }

        if (region.X < 0 || region.Y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(region), "裁剪坐标不能为负数。");
        }
    }

    private static void EnsureCropRegionInsideImage(CropRegion region, int imageWidth, int imageHeight)
    {
        if (region.Right > imageWidth || region.Bottom > imageHeight)
        {
            throw new ArgumentException("裁剪区域超出图像边界。", nameof(region));
        }
    }
}
