using Core.Models;

namespace Core.Abstractions;

/// <summary>
/// UI dump 解析器接口。
/// </summary>
public interface IUiDumpParser
{
    /// <summary>
    /// 解析 Android UI dump XML。
    /// </summary>
    Task<WidgetNode?> ParseAsync(string xmlContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按布局容器过滤规则扁平化有效节点。
    /// </summary>
    IReadOnlyList<WidgetNode> FilterNodes(WidgetNode root);

    /// <summary>
    /// 按条件查找节点。
    /// </summary>
    IReadOnlyList<WidgetNode> FindNodes(
        WidgetNode root,
        string? resourceId = null,
        string? text = null,
        string? contentDesc = null,
        string? className = null);

    /// <summary>
    /// 按截图像素坐标定位最深层节点。
    /// </summary>
    WidgetNode? FindNodeByCoordinate(WidgetNode root, int x, int y);

    /// <summary>
    /// 为目标节点生成优先级最高的 UiSelector 表达式。
    /// </summary>
    string GenerateUiSelector(WidgetNode node);
}
