using Core.Models.Desktop;

namespace Core.Abstractions.Desktop;

public interface IWindowStateService
{
    Task<WindowStateSnapshot?> RestoreAsync(string windowId, CancellationToken cancellationToken = default);

    Task PersistAsync(string windowId, WindowStateSnapshot snapshot, CancellationToken cancellationToken = default);
}
