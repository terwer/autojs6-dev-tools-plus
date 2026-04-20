using Avalonia.Controls;
using Avalonia.Input.Platform;
using Core.Abstractions.Desktop;

namespace Infrastructure.Platform;

/// <summary>
/// 基于 Avalonia 剪贴板的实现。
/// </summary>
public sealed class AvaloniaClipboardService : IClipboardService
{
    private readonly Func<TopLevel?> _topLevelAccessor;

    public AvaloniaClipboardService(Func<TopLevel?> topLevelAccessor)
    {
        _topLevelAccessor = topLevelAccessor ?? throw new ArgumentNullException(nameof(topLevelAccessor));
    }

    public async Task SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var clipboard = GetClipboard();
        await clipboard.SetTextAsync(text);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private IClipboard GetClipboard()
    {
        return _topLevelAccessor()?.Clipboard
               ?? throw new InvalidOperationException("当前未找到可用的 Avalonia TopLevel / Clipboard 上下文。");
    }
}
