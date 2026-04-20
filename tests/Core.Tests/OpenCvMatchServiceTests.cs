using Infrastructure.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Core.Tests;

public sealed class OpenCvMatchServiceTests
{
    [Fact]
    public async Task MatchTemplateAsync_ShouldReturnTmCcoeffNormedResult()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var screenshot = await CreateScreenshotAsync();
        var template = await CreateTemplateAsync();
        var service = new OpenCvMatchService();

        var result = await service.MatchTemplateAsync(screenshot, template, threshold: 0.99);

        Assert.NotNull(result);
        Assert.Equal(11, result!.X);
        Assert.Equal(7, result.Y);
        Assert.Equal(4, result.Width);
        Assert.Equal(4, result.Height);
        Assert.Equal("TM_CCOEFF_NORMED", result.Algorithm);
        Assert.True(result.IsMatch);
        Assert.InRange(result.Confidence, 0.99, 1.0);
    }

    [Fact]
    public async Task MatchTemplateMultiAsync_ShouldReturnAllHitsAboveThreshold()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var screenshot = await CreateRepeatedScreenshotAsync();
        var template = await CreateTemplateAsync();
        var service = new OpenCvMatchService();

        var results = await service.MatchTemplateMultiAsync(screenshot, template, threshold: 0.99);

        Assert.True(results.Count >= 2);
        Assert.Contains(results, item => item.X == 11 && item.Y == 7);
        Assert.Contains(results, item => item.X == 25 && item.Y == 16);
        Assert.All(results, item => Assert.Equal("TM_CCOEFF_NORMED", item.Algorithm));
    }

    private static async Task<byte[]> CreateScreenshotAsync()
    {
        using var image = new Image<Rgba32>(40, 30, new Rgba32(0, 0, 0));
        PaintPattern(image, 11, 7);
        return await EncodeAsync(image);
    }

    private static async Task<byte[]> CreateRepeatedScreenshotAsync()
    {
        using var image = new Image<Rgba32>(50, 35, new Rgba32(0, 0, 0));
        PaintPattern(image, 11, 7);
        PaintPattern(image, 25, 16);
        return await EncodeAsync(image);
    }

    private static async Task<byte[]> CreateTemplateAsync()
    {
        using var image = new Image<Rgba32>(4, 4, new Rgba32(0, 0, 0));
        PaintPattern(image, 0, 0);
        return await EncodeAsync(image);
    }

    private static void PaintPattern(Image<Rgba32> image, int originX, int originY)
    {
        var pattern = new[,]
        {
            { new Rgba32(255, 0, 0), new Rgba32(0, 255, 0), new Rgba32(0, 0, 255), new Rgba32(255, 255, 0) },
            { new Rgba32(255, 255, 255), new Rgba32(0, 0, 0), new Rgba32(0, 255, 255), new Rgba32(255, 0, 255) },
            { new Rgba32(128, 64, 32), new Rgba32(32, 64, 128), new Rgba32(200, 100, 50), new Rgba32(50, 100, 200) },
            { new Rgba32(16, 32, 48), new Rgba32(64, 96, 128), new Rgba32(144, 176, 208), new Rgba32(240, 220, 200) }
        };

        for (var y = 0; y < 4; y++)
        {
            for (var x = 0; x < 4; x++)
            {
                image[originX + x, originY + y] = pattern[y, x];
            }
        }
    }

    private static async Task<byte[]> EncodeAsync(Image<Rgba32> image)
    {
        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        return stream.ToArray();
    }
}
