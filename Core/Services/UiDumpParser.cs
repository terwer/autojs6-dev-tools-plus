using System.Text.RegularExpressions;
using System.Xml.Linq;
using Core.Abstractions;
using Core.Models;

namespace Core.Services;

/// <summary>
/// Android UI dump 解析器。
/// </summary>
public sealed partial class UiDumpParser : IUiDumpParser
{
    [GeneratedRegex(@"\[(\d+),(\d+)\]\[(\d+),(\d+)\]", RegexOptions.Compiled)]
    private static partial Regex BoundsRegex();

    public Task<WidgetNode?> ParseAsync(string xmlContent, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ParseInternal(xmlContent), cancellationToken);
    }

    public IReadOnlyList<WidgetNode> FilterNodes(WidgetNode root)
    {
        var result = new List<WidgetNode>();
        FilterNodesRecursive(root, result);
        return result;
    }

    public IReadOnlyList<WidgetNode> FindNodes(
        WidgetNode root,
        string? resourceId = null,
        string? text = null,
        string? contentDesc = null,
        string? className = null)
    {
        var result = new List<WidgetNode>();
        FindNodesRecursive(root, result, resourceId, text, contentDesc, className);
        return result;
    }

    public WidgetNode? FindNodeByCoordinate(WidgetNode root, int x, int y)
    {
        return FindNodeByCoordinateRecursive(root, x, y);
    }

    public string GenerateUiSelector(WidgetNode node)
    {
        string selector;

        if (!string.IsNullOrWhiteSpace(node.ResourceId))
        {
            selector = $"id(\"{EscapeJavaScript(node.ResourceId)}\")";
        }
        else if (!string.IsNullOrWhiteSpace(node.Text))
        {
            selector = $"text(\"{EscapeJavaScript(node.Text)}\")";
        }
        else if (!string.IsNullOrWhiteSpace(node.ContentDesc))
        {
            selector = $"desc(\"{EscapeJavaScript(node.ContentDesc)}\")";
        }
        else if (!string.IsNullOrWhiteSpace(node.ClassName))
        {
            selector = $"className(\"{EscapeJavaScript(node.ClassName)}\")";
        }
        else
        {
            selector = "selector()";
        }

        var (x, y, width, height) = node.BoundsRect;
        if (width > 0 && height > 0)
        {
            selector += $".boundsInside({x}, {y}, {x + width}, {y + height})";
        }

        return selector + ".findOne()";
    }

    private WidgetNode? ParseInternal(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            return null;
        }

        try
        {
            var document = XDocument.Parse(xmlContent, LoadOptions.PreserveWhitespace);
            var rootElement = document.Root;

            if (rootElement == null)
            {
                return null;
            }

            var firstNode = rootElement.Name.LocalName == "node"
                ? rootElement
                : rootElement.Element("node");

            return firstNode == null ? null : ParseNode(firstNode, 0);
        }
        catch
        {
            return null;
        }
    }

    private WidgetNode ParseNode(XElement element, int depth)
    {
        var bounds = element.Attribute("bounds")?.Value ?? string.Empty;

        var node = new WidgetNode
        {
            ClassName = element.Attribute("class")?.Value ?? string.Empty,
            ResourceId = NullIfEmpty(element.Attribute("resource-id")?.Value),
            Text = NullIfEmpty(element.Attribute("text")?.Value),
            ContentDesc = NullIfEmpty(element.Attribute("content-desc")?.Value),
            Clickable = ParseBooleanAttribute(element, "clickable"),
            Bounds = bounds,
            BoundsRect = ParseBounds(bounds),
            Package = NullIfEmpty(element.Attribute("package")?.Value),
            Checkable = ParseBooleanAttribute(element, "checkable"),
            Checked = ParseBooleanAttribute(element, "checked"),
            Focusable = ParseBooleanAttribute(element, "focusable"),
            Focused = ParseBooleanAttribute(element, "focused"),
            Scrollable = ParseBooleanAttribute(element, "scrollable"),
            LongClickable = ParseBooleanAttribute(element, "long-clickable"),
            Enabled = ParseBooleanAttribute(element, "enabled"),
            Depth = depth
        };

        foreach (var child in element.Elements().Where(x => x.Name.LocalName == "node"))
        {
            node.Children.Add(ParseNode(child, depth + 1));
        }

        return node;
    }

    private static bool ParseBooleanAttribute(XElement element, string attributeName)
    {
        return string.Equals(element.Attribute(attributeName)?.Value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static (int X, int Y, int Width, int Height) ParseBounds(string bounds)
    {
        var match = BoundsRegex().Match(bounds);
        if (!match.Success)
        {
            return (0, 0, 0, 0);
        }

        var x1 = int.Parse(match.Groups[1].Value);
        var y1 = int.Parse(match.Groups[2].Value);
        var x2 = int.Parse(match.Groups[3].Value);
        var y2 = int.Parse(match.Groups[4].Value);

        return (x1, y1, x2 - x1, y2 - y1);
    }

    private static void FilterNodesRecursive(WidgetNode node, ICollection<WidgetNode> result)
    {
        var isLayoutContainer = node.ClassName.Contains("Layout", StringComparison.Ordinal) &&
                                string.IsNullOrWhiteSpace(node.ResourceId) &&
                                string.IsNullOrWhiteSpace(node.Text) &&
                                string.IsNullOrWhiteSpace(node.ContentDesc) &&
                                !node.Clickable;

        if (!isLayoutContainer)
        {
            result.Add(node);
        }

        foreach (var child in node.Children)
        {
            FilterNodesRecursive(child, result);
        }
    }

    private static void FindNodesRecursive(
        WidgetNode node,
        ICollection<WidgetNode> result,
        string? resourceId,
        string? text,
        string? contentDesc,
        string? className)
    {
        var matches = (resourceId == null || string.Equals(node.ResourceId, resourceId, StringComparison.Ordinal)) &&
                      (text == null || string.Equals(node.Text, text, StringComparison.Ordinal)) &&
                      (contentDesc == null || string.Equals(node.ContentDesc, contentDesc, StringComparison.Ordinal)) &&
                      (className == null || string.Equals(node.ClassName, className, StringComparison.Ordinal));

        if (matches)
        {
            result.Add(node);
        }

        foreach (var child in node.Children)
        {
            FindNodesRecursive(child, result, resourceId, text, contentDesc, className);
        }
    }

    private static WidgetNode? FindNodeByCoordinateRecursive(WidgetNode node, int x, int y)
    {
        var (nodeX, nodeY, width, height) = node.BoundsRect;
        var contains = x >= nodeX && x < nodeX + width && y >= nodeY && y < nodeY + height;
        if (!contains)
        {
            return null;
        }

        foreach (var child in node.Children)
        {
            var childMatch = FindNodeByCoordinateRecursive(child, x, y);
            if (childMatch != null)
            {
                return childMatch;
            }
        }

        return node;
    }

    private static string EscapeJavaScript(string input)
    {
        return input
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }
}
