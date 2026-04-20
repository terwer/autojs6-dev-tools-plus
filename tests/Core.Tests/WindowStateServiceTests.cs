using Core.Models.Desktop;
using Infrastructure.Platform;

namespace Core.Tests;

public sealed class WindowStateServiceTests
{
    [Fact]
    public async Task PersistAsync_ThenRestoreAsync_ShouldRoundTripSnapshot()
    {
        var storageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new JsonWindowStateService(storageDirectory);
        var snapshot = new WindowStateSnapshot(1440, 900, 120, 80, DesktopWindowState.Maximized);

        try
        {
            await service.PersistAsync("main/window", snapshot);
            var restored = await service.RestoreAsync("main/window");

            Assert.Equal(snapshot, restored);
        }
        finally
        {
            if (Directory.Exists(storageDirectory))
            {
                Directory.Delete(storageDirectory, recursive: true);
            }
        }
    }
}
