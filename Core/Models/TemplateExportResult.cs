namespace Core.Models;

/// <summary>
/// 模板导出结果。
/// </summary>
public sealed record TemplateExportResult(
    byte[] TemplateBytes,
    string MetadataJson,
    CropRegion Region,
    ImageInfo OriginalImage,
    ImageInfo TemplateImage);
