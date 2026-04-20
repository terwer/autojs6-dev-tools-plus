using Core.Models;
using Core.Services;

namespace Core.Tests;

public sealed class AutoJs6CodeGeneratorTests
{
    [Fact]
    public void GenerateImageModeCode_ShouldNormalizePath_UseRegion_AndUseVarInLoop()
    {
        var generator = new AutoJS6CodeGenerator();
        var options = new AutoJS6CodeOptions
        {
            Mode = CodeGenerationMode.Image,
            TemplatePath = @".\\assets\\login_button.png",
            VariablePrefix = "login",
            RetryCount = 2,
            Region = new CropRegion { X = 10, Y = 20, Width = 30, Height = 40 }
        };

        var code = generator.GenerateImageModeCode(options);

        Assert.Contains("images.read(\"./assets/login_button.png\")", code);
        Assert.Contains("region: [10, 20, 30, 40]", code);
        Assert.Contains("for (var i = 0; i < 2; i++)", code);
        Assert.DoesNotContain("let ", code);
        Assert.DoesNotContain("const ", code);

        var validation = generator.ValidateCode(code);
        Assert.True(validation.IsValid, string.Join(Environment.NewLine, validation.Errors));
    }

    [Fact]
    public void GenerateWidgetModeCode_ShouldGenerateFallbackSelectorChain()
    {
        var generator = new AutoJS6CodeGenerator();
        var options = new AutoJS6CodeOptions
        {
            Mode = CodeGenerationMode.Widget,
            VariablePrefix = "widget",
            Widget = new WidgetNode
            {
                ClassName = "android.widget.Button",
                ResourceId = "com.demo:id/login",
                Text = "登录",
                ContentDesc = "登录按钮",
                Bounds = "[100,200][400,320]",
                BoundsRect = (100, 200, 300, 120),
                Clickable = true,
                Enabled = true
            }
        };

        var code = generator.GenerateWidgetModeCode(options);

        Assert.Contains("var widget = id(\"com.demo:id/login\").findOne();", code);
        Assert.Contains("if (!widget) widget = text(\"登录\").findOne();", code);
        Assert.Contains("if (!widget) widget = desc(\"登录按钮\").findOne();", code);
        Assert.Contains("boundsInside(100, 200, 400, 320)", code);
    }

    [Fact]
    public void ValidateCode_ShouldRejectLoopLetAndBackslashPaths()
    {
        var generator = new AutoJS6CodeGenerator();
        const string code = """
for (var i = 0; i < 3; i++) {
  let bad = 1;
  var image = images.read(".\\assets\\a.png");
  var screen = captureScreen();
  var other = captureScreen();
}
""";

        var validation = generator.ValidateCode(code);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.Contains("Rhino"));
        Assert.Contains(validation.Errors, error => error.Contains("正斜杠"));
        Assert.Contains(validation.Errors, error => error.Contains("OOM"));
    }
}
