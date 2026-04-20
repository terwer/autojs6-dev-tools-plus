namespace Core.Abstractions.Desktop;

public interface IClipboardService
{
    Task SetTextAsync(string text, CancellationToken cancellationToken = default);
}
