using Core.Models.Desktop;

namespace Core.Abstractions.Desktop;

public interface IHotkeyService
{
    ValueTask RegisterAsync(
        string commandId,
        HotkeyGesture gesture,
        Func<CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);

    ValueTask UnregisterAsync(string commandId, CancellationToken cancellationToken = default);
}
