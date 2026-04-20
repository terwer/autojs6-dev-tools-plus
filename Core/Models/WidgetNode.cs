namespace Core.Models;

/// <summary>
/// UI 控件节点。
/// </summary>
public sealed class WidgetNode
{
    /// <summary>
    /// 控件类名，例如 android.widget.TextView。
    /// </summary>
    public required string ClassName { get; init; }

    /// <summary>
    /// 资源 ID。
    /// </summary>
    public string? ResourceId { get; init; }

    /// <summary>
    /// 文本内容。
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// content-desc。
    /// </summary>
    public string? ContentDesc { get; init; }

    /// <summary>
    /// 是否可点击。
    /// </summary>
    public bool Clickable { get; init; }

    /// <summary>
    /// 原始 bounds 字符串，例如 [0,0][100,50]。
    /// </summary>
    public required string Bounds { get; init; }

    /// <summary>
    /// 解析后的 bounds 矩形（x, y, width, height）。
    /// </summary>
    public (int X, int Y, int Width, int Height) BoundsRect { get; set; }

    /// <summary>
    /// 所属包名。
    /// </summary>
    public string? Package { get; init; }

    public bool Checkable { get; init; }

    public bool Checked { get; init; }

    public bool Focusable { get; init; }

    public bool Focused { get; init; }

    public bool Scrollable { get; init; }

    public bool LongClickable { get; init; }

    public bool Enabled { get; init; }

    /// <summary>
    /// 树深度，用于 TreeView 与过滤逻辑。
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    /// 子节点集合。
    /// </summary>
    public List<WidgetNode> Children { get; init; } = new();
}
