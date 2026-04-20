using Core.Models.Desktop;

namespace Core.Tests;

public sealed class DesktopContractsTests
{
    [Fact]
    public void HotkeyGesture_ToString_ShouldReturnOrderedShortcut()
    {
        var gesture = new HotkeyGesture("K", Ctrl: true, Shift: true, Alt: true);

        Assert.Equal("Ctrl+Shift+Alt+K", gesture.ToString());
    }

    [Fact]
    public void SaveFileResult_Cancelled_ShouldRepresentUserCancellation()
    {
        Assert.False(SaveFileResult.Cancelled.Confirmed);
        Assert.Null(SaveFileResult.Cancelled.FilePath);
    }
}
