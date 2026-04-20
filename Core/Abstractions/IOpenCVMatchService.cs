using Core.Models;

namespace Core.Abstractions;

/// <summary>
/// OpenCV 模板匹配服务抽象。
/// </summary>
public interface IOpenCVMatchService
{
    Task<MatchResult?> MatchTemplateAsync(
        byte[] screenshot,
        byte[] template,
        double threshold = 0.8,
        CropRegion? region = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MatchResult>> MatchTemplateMultiAsync(
        byte[] screenshot,
        byte[] template,
        double threshold = 0.8,
        CropRegion? region = null,
        CancellationToken cancellationToken = default);

    bool ValidateTemplate(byte[] template);
}
