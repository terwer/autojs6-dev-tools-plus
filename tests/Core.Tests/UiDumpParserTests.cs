using Core.Services;

namespace Core.Tests;

public sealed class UiDumpParserTests
{
    private const string SampleXml = """
<hierarchy rotation="0">
  <node index="0" text="" resource-id="" class="android.widget.FrameLayout" package="demo" content-desc="" clickable="false" checkable="false" checked="false" enabled="true" focusable="false" focused="false" scrollable="false" long-clickable="false" bounds="[0,0][1080,2400]">
    <node index="1" text="" resource-id="" class="android.widget.LinearLayout" package="demo" content-desc="" clickable="false" checkable="false" checked="false" enabled="true" focusable="false" focused="false" scrollable="false" long-clickable="false" bounds="[0,0][1080,2400]">
      <node index="2" text="登录" resource-id="com.demo:id/login" class="android.widget.Button" package="demo" content-desc="登录按钮" clickable="true" checkable="false" checked="false" enabled="true" focusable="true" focused="false" scrollable="false" long-clickable="false" bounds="[100,200][400,320]" />
    </node>
    <node index="3" text="容器" resource-id="com.demo:id/panel" class="android.widget.LinearLayout" package="demo" content-desc="" clickable="true" checkable="false" checked="false" enabled="true" focusable="false" focused="false" scrollable="false" long-clickable="false" bounds="[50,400][800,900]" />
  </node>
</hierarchy>
""";

    [Fact]
    public async Task ParseAsync_ShouldParseBoundsAndChildren()
    {
        var parser = new UiDumpParser();

        var root = await parser.ParseAsync(SampleXml);

        Assert.NotNull(root);
        Assert.Equal("android.widget.FrameLayout", root!.ClassName);
        Assert.Single(root.Children[0].Children);

        var button = root.Children[0].Children[0];
        Assert.Equal((100, 200, 300, 120), button.BoundsRect);
        Assert.Equal("com.demo:id/login", button.ResourceId);
        Assert.Equal("登录", button.Text);
    }

    [Fact]
    public async Task FilterNodes_ShouldSkipEmptyLayoutContainers_ButKeepClickableLayout()
    {
        var parser = new UiDumpParser();
        var root = await parser.ParseAsync(SampleXml);

        var nodes = parser.FilterNodes(root!);

        Assert.DoesNotContain(nodes, node => node.ClassName == "android.widget.LinearLayout" && node.ResourceId is null && !node.Clickable);
        Assert.Contains(nodes, node => node.ResourceId == "com.demo:id/login");
        Assert.Contains(nodes, node => node.ResourceId == "com.demo:id/panel" && node.Clickable);
    }

    [Fact]
    public async Task FindNodeByCoordinate_ShouldReturnDeepestNode()
    {
        var parser = new UiDumpParser();
        var root = await parser.ParseAsync(SampleXml);

        var node = parser.FindNodeByCoordinate(root!, 120, 230);

        Assert.NotNull(node);
        Assert.Equal("com.demo:id/login", node!.ResourceId);
    }

    [Fact]
    public async Task GenerateUiSelector_ShouldPreferIdAndAppendBounds()
    {
        var parser = new UiDumpParser();
        var root = await parser.ParseAsync(SampleXml);
        var button = root!.Children[0].Children[0];

        var selector = parser.GenerateUiSelector(button);

        Assert.Equal("id(\"com.demo:id/login\").boundsInside(100, 200, 400, 320).findOne()", selector);
    }
}
