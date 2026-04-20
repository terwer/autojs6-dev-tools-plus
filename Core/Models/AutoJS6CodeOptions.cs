namespace Core.Models;

/// <summary>
/// AutoJS6 代码生成选项。
/// </summary>
public sealed class AutoJS6CodeOptions
{
    /// <summary>
    /// 代码生成模式。
    /// </summary>
    public required CodeGenerationMode Mode { get; init; }

    /// <summary>
    /// 模板匹配阈值（0.0 - 1.0）。
    /// </summary>
    public double Threshold { get; init; } = 0.8;

    /// <summary>
    /// 重试次数。
    /// </summary>
    public int RetryCount { get; init; } = 3;

    /// <summary>
    /// 超时时间（毫秒）。
    /// </summary>
    public int TimeoutMilliseconds { get; init; } = 5000;

    /// <summary>
    /// 变量名前缀。
    /// </summary>
    public string VariablePrefix { get; init; } = "target";

    /// <summary>
    /// 模板文件路径（图像模式）。
    /// </summary>
    public string? TemplatePath { get; init; }

    /// <summary>
    /// 搜索区域。
    /// </summary>
    public CropRegion? Region { get; init; }

    /// <summary>
    /// 目标控件（控件模式）。
    /// </summary>
    public WidgetNode? Widget { get; init; }

    /// <summary>
    /// 是否生成重试逻辑。
    /// </summary>
    public bool GenerateRetryLogic { get; init; } = true;

    /// <summary>
    /// 是否生成超时逻辑。
    /// </summary>
    public bool GenerateTimeoutLogic { get; init; } = true;

    /// <summary>
    /// 是否生成日志输出。
    /// </summary>
    public bool GenerateLogging { get; init; }

    /// <summary>
    /// 是否生成图像回收代码。
    /// </summary>
    public bool GenerateImageRecycle { get; init; } = true;

    /// <summary>
    /// 屏幕方向，例如 landscape / portrait。
    /// </summary>
    public string? Orientation { get; init; }
}
