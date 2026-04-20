using System.Diagnostics;
using Core.Abstractions;
using Core.Models;
using OpenCvSharp;

namespace Infrastructure.Imaging;

/// <summary>
/// 基于 OpenCvSharp 的模板匹配实现。
/// </summary>
public sealed class OpenCvMatchService : IOpenCVMatchService
{
    private const string AlgorithmName = "TM_CCOEFF_NORMED";

    public async Task<MatchResult?> MatchTemplateAsync(
        byte[] screenshot,
        byte[] template,
        double threshold = 0.8,
        CropRegion? region = null,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(screenshot, template, threshold);

        return await Task.Run(() =>
        {
            using var screenshotMat = Mat.FromImageData(screenshot);
            using var templateMat = Mat.FromImageData(template);
            EnsureTemplateCanSearch(templateMat, screenshotMat, region);

            using var searchScope = CreateSearchScope(screenshotMat, region);
            using var resultMat = new Mat();
            var stopwatch = Stopwatch.StartNew();

            Cv2.MatchTemplate(searchScope.SearchMat, templateMat, resultMat, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(resultMat, out _, out var maxValue, out _, out var maxLocation);
            stopwatch.Stop();

            return new MatchResult
            {
                X = maxLocation.X + searchScope.OffsetX,
                Y = maxLocation.Y + searchScope.OffsetY,
                Width = templateMat.Width,
                Height = templateMat.Height,
                Confidence = maxValue,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                IsMatch = maxValue >= threshold,
                Algorithm = AlgorithmName,
                Threshold = threshold
            };
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<MatchResult>> MatchTemplateMultiAsync(
        byte[] screenshot,
        byte[] template,
        double threshold = 0.8,
        CropRegion? region = null,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(screenshot, template, threshold);

        return await Task.Run<IReadOnlyList<MatchResult>>(() =>
        {
            using var screenshotMat = Mat.FromImageData(screenshot);
            using var templateMat = Mat.FromImageData(template);
            EnsureTemplateCanSearch(templateMat, screenshotMat, region);

            using var searchScope = CreateSearchScope(screenshotMat, region);
            using var resultMat = new Mat();
            var stopwatch = Stopwatch.StartNew();

            Cv2.MatchTemplate(searchScope.SearchMat, templateMat, resultMat, TemplateMatchModes.CCoeffNormed);
            stopwatch.Stop();

            var results = new List<MatchResult>();
            for (var y = 0; y < resultMat.Rows; y++)
            {
                for (var x = 0; x < resultMat.Cols; x++)
                {
                    var confidence = resultMat.At<float>(y, x);
                    if (confidence < threshold)
                    {
                        continue;
                    }

                    results.Add(new MatchResult
                    {
                        X = x + searchScope.OffsetX,
                        Y = y + searchScope.OffsetY,
                        Width = templateMat.Width,
                        Height = templateMat.Height,
                        Confidence = confidence,
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                        IsMatch = true,
                        Algorithm = AlgorithmName,
                        Threshold = threshold
                    });
                }
            }

            return results
                .OrderByDescending(result => result.Confidence)
                .ToArray();
        }, cancellationToken);
    }

    public bool ValidateTemplate(byte[] template)
    {
        ArgumentNullException.ThrowIfNull(template);

        try
        {
            using var mat = Mat.FromImageData(template);
            return !mat.Empty() && mat.Width > 0 && mat.Height > 0;
        }
        catch
        {
            return false;
        }
    }

    private static SearchScope CreateSearchScope(Mat screenshotMat, CropRegion? region)
    {
        if (region is null)
        {
            return new SearchScope(screenshotMat, null, 0, 0);
        }

        var searchRect = new Rect(region.X, region.Y, region.Width, region.Height);
        var searchMat = new Mat(screenshotMat, searchRect);
        return new SearchScope(searchMat, searchMat, region.X, region.Y);
    }

    private static void EnsureTemplateCanSearch(Mat templateMat, Mat screenshotMat, CropRegion? region)
    {
        var scopeWidth = region?.Width ?? screenshotMat.Width;
        var scopeHeight = region?.Height ?? screenshotMat.Height;

        if (templateMat.Width > scopeWidth || templateMat.Height > scopeHeight)
        {
            throw new InvalidOperationException("模板尺寸不能大于搜索区域。");
        }

        if (region is null)
        {
            return;
        }

        if (region.X < 0 || region.Y < 0 || region.Right > screenshotMat.Width || region.Bottom > screenshotMat.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(region), "搜索区域超出截图边界。");
        }
    }

    private static void ValidateInputs(byte[] screenshot, byte[] template, double threshold)
    {
        ArgumentNullException.ThrowIfNull(screenshot);
        ArgumentNullException.ThrowIfNull(template);

        if (screenshot.Length == 0)
        {
            throw new ArgumentException("截图数据不能为空。", nameof(screenshot));
        }

        if (template.Length == 0)
        {
            throw new ArgumentException("模板数据不能为空。", nameof(template));
        }

        if (threshold is < 0d or > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), "threshold 必须在 0.0 - 1.0 之间。");
        }
    }

    private sealed record SearchScope(Mat SearchMat, Mat? OwnedMat, int OffsetX, int OffsetY) : IDisposable
    {
        public void Dispose()
        {
            OwnedMat?.Dispose();
        }
    }
}
