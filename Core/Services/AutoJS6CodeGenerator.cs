using System.Text;
using Core.Abstractions;
using Core.Models;

namespace Core.Services;

/// <summary>
/// AutoJS6 代码生成器。
/// </summary>
public sealed class AutoJS6CodeGenerator : ICodeGenerator
{
    public string GenerateImageModeCode(AutoJS6CodeOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TemplatePath))
        {
            throw new ArgumentException("Image mode requires TemplatePath.", nameof(options));
        }

        var templatePath = NormalizePath(options.TemplatePath);
        var prefix = SanitizeVariablePrefix(options.VariablePrefix);
        var templateVariable = prefix + "Template";
        var foundVariable = prefix + "Found";

        var sb = new StringBuilder();
        sb.AppendLine("// 图像模式：基于 images.findImage() 生成");
        sb.AppendLine("if (!requestScreenCapture()) {");
        sb.AppendLine("  toast(\"请求截图权限失败\");");
        sb.AppendLine("  exit();");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"var {templateVariable} = images.read(\"{templatePath}\");");
        sb.AppendLine($"if (!{templateVariable}) {{");
        sb.AppendLine("  toast(\"模板图像加载失败\");");
        sb.AppendLine("  exit();");
        sb.AppendLine("}");
        sb.AppendLine();

        if (options.GenerateRetryLogic)
        {
            sb.AppendLine($"var {foundVariable} = false;");
            sb.AppendLine($"for (var i = 0; i < {Math.Max(1, options.RetryCount)}; i++) {{");
            AppendImageMatchBlock(sb, options, prefix, templateVariable, insideLoop: true, indent: "  ");
            sb.AppendLine("  if (result) {");
            sb.AppendLine($"    {foundVariable} = true;");
            sb.AppendLine("    break;");
            sb.AppendLine("  }");
            sb.AppendLine("  sleep(1000);");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"if (!{foundVariable}) {{");
            sb.AppendLine("  toast(\"未找到目标\");");
            sb.AppendLine("  exit();");
            sb.AppendLine("}");
        }
        else
        {
            AppendImageMatchBlock(sb, options, prefix, templateVariable, insideLoop: false, indent: string.Empty);
            sb.AppendLine("if (!result) {");
            sb.AppendLine("  toast(\"未找到目标\");");
            sb.AppendLine("  exit();");
            sb.AppendLine("}");
        }

        if (options.GenerateImageRecycle)
        {
            sb.AppendLine();
            sb.AppendLine($"{templateVariable}.recycle();");
        }

        return FormatCode(sb.ToString());
    }

    public string GenerateWidgetModeCode(AutoJS6CodeOptions options)
    {
        if (options.Widget == null)
        {
            throw new ArgumentException("Widget mode requires Widget.", nameof(options));
        }

        var widget = options.Widget;
        var prefix = SanitizeVariablePrefix(options.VariablePrefix);
        var variableName = prefix;
        var selectors = BuildWidgetSelectorCandidates(widget);
        if (selectors.Count == 0)
        {
            throw new InvalidOperationException("Widget selector candidates cannot be empty.");
        }

        var sb = new StringBuilder();
        sb.AppendLine("// 控件模式：基于 UiSelector 生成");
        sb.AppendLine($"var {variableName} = {selectors[0]}.findOne();");

        for (var index = 1; index < selectors.Count; index++)
        {
            sb.AppendLine($"if (!{variableName}) {variableName} = {selectors[index]}.findOne();");
        }

        sb.AppendLine($"if ({variableName}) {{");
        sb.AppendLine($"  {variableName}.click();");

        if (options.GenerateLogging)
        {
            sb.AppendLine("  log(\"控件点击成功\");");
        }

        sb.AppendLine("} else {");
        sb.AppendLine("  toast(\"未找到控件\");");
        sb.AppendLine("}");

        return FormatCode(sb.ToString());
    }

    public string GenerateFullScript(AutoJS6CodeOptions options)
    {
        var body = options.Mode switch
        {
            CodeGenerationMode.Image => GenerateImageModeCode(options),
            CodeGenerationMode.Widget => GenerateWidgetModeCode(options),
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };

        var sb = new StringBuilder();
        sb.AppendLine("\"ui\";");
        sb.AppendLine();
        sb.AppendLine("// AutoJS6 自动生成脚本");
        sb.AppendLine($"// 模式: {options.Mode}");
        sb.AppendLine($"// 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.Append(body);
        return FormatCode(sb.ToString());
    }

    public string FormatCode(string code, int indentSize = 2)
    {
        var lines = code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var builder = new StringBuilder();
        var indentLevel = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                builder.AppendLine();
                continue;
            }

            if (line.StartsWith("}", StringComparison.Ordinal))
            {
                indentLevel = Math.Max(0, indentLevel - 1);
            }

            builder.Append(' ', indentLevel * indentSize);
            builder.AppendLine(line);

            if (line.EndsWith("{", StringComparison.Ordinal))
            {
                indentLevel++;
            }
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    public (bool IsValid, IReadOnlyList<string> Errors) ValidateCode(string code)
    {
        var errors = new List<string>();
        var lines = code.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var loopBraceDepth = -1;
        var braceDepth = 0;
        var captureCountInLoop = 0;

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if ((line.StartsWith("for", StringComparison.Ordinal) || line.StartsWith("while", StringComparison.Ordinal)) && line.Contains('{', StringComparison.Ordinal))
            {
                loopBraceDepth = braceDepth;
                captureCountInLoop = 0;
            }

            if (loopBraceDepth >= 0 && (line.Contains("const ", StringComparison.Ordinal) || line.Contains("let ", StringComparison.Ordinal)))
            {
                errors.Add($"第 {index + 1} 行: Rhino 循环体内禁止 const/let，请使用 var。");
            }

            if (loopBraceDepth >= 0 && line.Contains("captureScreen()", StringComparison.Ordinal))
            {
                captureCountInLoop++;
                if (captureCountInLoop > 1)
                {
                    errors.Add($"第 {index + 1} 行: 单轮循环只允许一次 captureScreen()，避免 OOM。");
                }
            }

            if (line.Contains("images.read(\"", StringComparison.Ordinal) && line.Contains('\\'))
            {
                errors.Add($"第 {index + 1} 行: 模板路径必须使用正斜杠。");
            }

            braceDepth += Count(line, '{');
            braceDepth -= Count(line, '}');

            if (loopBraceDepth >= 0 && braceDepth <= loopBraceDepth)
            {
                loopBraceDepth = -1;
            }
        }

        if (code.Contains("images.findImage", StringComparison.Ordinal) && !code.Contains("requestScreenCapture()", StringComparison.Ordinal))
        {
            errors.Add("图像模式代码必须先调用 requestScreenCapture()。");
        }

        if (code.Contains("images.findImage", StringComparison.Ordinal) && code.Contains("captureScreen()", StringComparison.Ordinal) && !code.Contains("recycle()", StringComparison.Ordinal))
        {
            errors.Add("图像模式代码必须显式回收图像对象以降低 OOM 风险。");
        }

        return (errors.Count == 0, errors);
    }

    private static void AppendImageMatchBlock(
        StringBuilder sb,
        AutoJS6CodeOptions options,
        string prefix,
        string templateVariable,
        bool insideLoop,
        string indent)
    {
        sb.AppendLine(indent + "var screen = captureScreen();");
        sb.AppendLine(indent + "if (!screen) {");
        sb.AppendLine(indent + "  sleep(1000);");

        if (!insideLoop)
        {
            sb.AppendLine(indent + "  toast(\"截图失败\");");
            sb.AppendLine(indent + "  exit();");
        }
        else
        {
            sb.AppendLine(indent + "  continue;");
        }

        sb.AppendLine(indent + "}");
        sb.AppendLine(indent + "try {");
        sb.AppendLine(indent + "  var result = images.findImage(screen, " + templateVariable + ", {");
        sb.AppendLine(indent + $"    threshold: {options.Threshold:F2}" + (options.Region != null ? "," : string.Empty));

        if (options.Region != null)
        {
            var region = options.Region;
            sb.AppendLine(indent + $"    region: [{region.X}, {region.Y}, {region.Width}, {region.Height}]");
        }

        sb.AppendLine(indent + "  });");
        sb.AppendLine(indent + "  if (result) {");
        sb.AppendLine(indent + $"    var {prefix}ClickX = result.x + {templateVariable}.width / 2;");
        sb.AppendLine(indent + $"    var {prefix}ClickY = result.y + {templateVariable}.height / 2;");
        sb.AppendLine(indent + $"    click({prefix}ClickX, {prefix}ClickY);");

        if (options.GenerateLogging)
        {
            sb.AppendLine(indent + "    log(\"图像点击成功\");");
        }

        sb.AppendLine(indent + "  }");
        sb.AppendLine(indent + "} finally {");

        if (options.GenerateImageRecycle)
        {
            sb.AppendLine(indent + "  screen.recycle();");
        }

        sb.AppendLine(indent + "}");
        sb.AppendLine();
    }

    private static List<string> BuildWidgetSelectorCandidates(WidgetNode widget)
    {
        var selectors = new List<string>();
        var (x, y, width, height) = widget.BoundsRect;
        var hasBounds = width > 0 && height > 0;
        var boundsSuffix = hasBounds ? $".boundsInside({x}, {y}, {x + width}, {y + height})" : string.Empty;

        if (!string.IsNullOrWhiteSpace(widget.ResourceId))
        {
            selectors.Add($"id(\"{EscapeJavaScript(widget.ResourceId)}\")");
            if (hasBounds)
            {
                selectors.Add($"id(\"{EscapeJavaScript(widget.ResourceId)}\"){boundsSuffix}");
            }
        }

        if (!string.IsNullOrWhiteSpace(widget.Text))
        {
            selectors.Add($"text(\"{EscapeJavaScript(widget.Text)}\")");
            if (hasBounds)
            {
                selectors.Add($"text(\"{EscapeJavaScript(widget.Text)}\"){boundsSuffix}");
            }
        }

        if (!string.IsNullOrWhiteSpace(widget.ContentDesc))
        {
            selectors.Add($"desc(\"{EscapeJavaScript(widget.ContentDesc)}\")");
            if (hasBounds)
            {
                selectors.Add($"desc(\"{EscapeJavaScript(widget.ContentDesc)}\"){boundsSuffix}");
            }
        }

        if (selectors.Count == 0 && !string.IsNullOrWhiteSpace(widget.ClassName))
        {
            selectors.Add($"className(\"{EscapeJavaScript(widget.ClassName)}\")" + boundsSuffix);
        }

        return selectors.Distinct(StringComparer.Ordinal).ToList();
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Replace('\\', '/');

        while (normalized.Contains("//", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        }

        return normalized;
    }

    private static string SanitizeVariablePrefix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "target";
        }

        var chars = value.Where(char.IsLetterOrDigit).ToArray();
        return chars.Length == 0 ? "target" : new string(chars);
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

    private static int Count(string line, char ch)
    {
        var total = 0;
        foreach (var current in line)
        {
            if (current == ch)
            {
                total++;
            }
        }

        return total;
    }
}
