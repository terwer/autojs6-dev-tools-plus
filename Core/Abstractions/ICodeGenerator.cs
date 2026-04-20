using Core.Models;

namespace Core.Abstractions;

/// <summary>
/// AutoJS6 代码生成器接口。
/// </summary>
public interface ICodeGenerator
{
    string GenerateImageModeCode(AutoJS6CodeOptions options);

    string GenerateWidgetModeCode(AutoJS6CodeOptions options);

    string GenerateFullScript(AutoJS6CodeOptions options);

    string FormatCode(string code, int indentSize = 2);

    (bool IsValid, IReadOnlyList<string> Errors) ValidateCode(string code);
}
