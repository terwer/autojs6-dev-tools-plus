using Infrastructure.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Core.Tests;

public sealed class InfrastructureImagingTests
{
    [Fact]
    public async Task DecodeAsync_ShouldReturnRgbaPixelsAndDimensions()
    {
        var imageBytes = await CreatePngAsync(3, 2, image =>
        {
            image[0, 0] = new Rgba32(255, 0, 0);
            image[1, 0] = new Rgba32(0, 255, 0);
            image[2, 0] = new Rgba32(0, 0, 255);
            image[0, 1] = new Rgba32(255, 255, 0);
            image[1, 1] = new Rgba32(0, 255, 255);
            image[2, 1] = new Rgba32(255, 0, 255);
        });

        var service = new ImageProcessingService();
        var decoded = await service.DecodeAsync(imageBytes);

        Assert.Equal(3, decoded.Width);
        Assert.Equal(2, decoded.Height);
        Assert.Equal("RGBA8888", decoded.PixelFormat);
        Assert.Equal(3 * 2 * 4, decoded.PixelData.Length);
        Assert.Equal(255, decoded.PixelData[0]);
        Assert.Equal(0, decoded.PixelData[1]);
        Assert.Equal(0, decoded.PixelData[2]);
    }

    [Fact]
    public async Task DownsampleAsync_ShouldRespectMaxDimensions()
    {
        var imageBytes = await CreateSolidPngAsync(400, 200, new Rgba32(32, 64, 96));
        var service = new ImageProcessingService();

        var result = await service.DownsampleAsync(imageBytes, maxWidth: 100, maxHeight: 100);

        Assert.True(result.WasDownsampled);
        Assert.Equal(0.25d, result.ScaleFactor, 3);
        Assert.Equal(400, result.Original.Width);
        Assert.Equal(200, result.Original.Height);
        Assert.Equal(100, result.Current.Width);
        Assert.Equal(50, result.Current.Height);
        Assert.NotEmpty(result.EncodedBytes);
    }

    [Fact]
    public async Task ExportTemplateAsync_ShouldCropPngAndEmitMetadata()
    {
        var imageBytes = await CreateSolidPngAsync(8, 6, new Rgba32(200, 100, 50));
        var service = new ImageProcessingService();

        var result = await service.ExportTemplateAsync(
            imageBytes,
            new Core.Models.CropRegion { X = 2, Y = 1, Width = 3, Height = 4 },
            "login_button");

        Assert.Equal(8, result.OriginalImage.Width);
        Assert.Equal(6, result.OriginalImage.Height);
        Assert.Equal(3, result.TemplateImage.Width);
        Assert.Equal(4, result.TemplateImage.Height);
        Assert.Contains("\"templateName\": \"login_button\"", result.MetadataJson);
        Assert.Contains("\"originalWidth\": 8", result.MetadataJson);
        Assert.Contains("\"x\": 2", result.MetadataJson);
        Assert.Contains("\"height\": 4", result.MetadataJson);
        Assert.NotEmpty(result.TemplateBytes);
    }

    private static async Task<byte[]> CreateSolidPngAsync(int width, int height, Rgba32 color)
    {
        return await CreatePngAsync(width, height, image =>
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    image[x, y] = color;
                }
            }
        });
    }

    private static async Task<byte[]> CreatePngAsync(int width, int height, Action<Image<Rgba32>> fill)
    {
        using var image = new Image<Rgba32>(width, height);
        fill(image);

        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        return stream.ToArray();
    }
}
