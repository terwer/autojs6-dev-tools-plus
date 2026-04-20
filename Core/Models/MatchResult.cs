namespace Core.Models;

/// <summary>
/// 模板匹配结果。
/// </summary>
public sealed class MatchResult
{
    /// <summary>
    /// 匹配位置左上角 X。
    /// </summary>
    public required int X { get; init; }

    /// <summary>
    /// 匹配位置左上角 Y。
    /// </summary>
    public required int Y { get; init; }

    /// <summary>
    /// 模板宽度。
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// 模板高度。
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// 置信度（0.0 - 1.0）。
    /// </summary>
    public required double Confidence { get; init; }

    /// <summary>
    /// 匹配耗时（毫秒）。
    /// </summary>
    public required long ElapsedMilliseconds { get; init; }

    /// <summary>
    /// 是否命中当前阈值。
    /// </summary>
    public bool IsMatch { get; init; }

    /// <summary>
    /// 匹配算法名称，例如 TM_CCOEFF_NORMED。
    /// </summary>
    public string? Algorithm { get; init; }

    /// <summary>
    /// 实际使用阈值。
    /// </summary>
    public double? Threshold { get; init; }

    /// <summary>
    /// 中心点击点 X。
    /// </summary>
    public int ClickX => X + Width / 2;

    /// <summary>
    /// 中心点击点 Y。
    /// </summary>
    public int ClickY => Y + Height / 2;
}
