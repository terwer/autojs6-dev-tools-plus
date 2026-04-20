using System.Text.Json;
using Core.Abstractions.Desktop;
using Core.Models.Desktop;

namespace Infrastructure.Platform;

/// <summary>
/// 基于本地 JSON 文件的窗口状态持久化实现。
/// </summary>
public sealed class JsonWindowStateService : IWindowStateService
{
    private readonly string _storageDirectory;

    public JsonWindowStateService(string? storageDirectory = null)
    {
        _storageDirectory = storageDirectory
                            ?? Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "autojs6-dev-tools-plus",
                                "window-state");
    }

    public async Task<WindowStateSnapshot?> RestoreAsync(string windowId, CancellationToken cancellationToken = default)
    {
        var path = GetSnapshotPath(windowId);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<WindowStateSnapshot>(stream, cancellationToken: cancellationToken);
    }

    public async Task PersistAsync(string windowId, WindowStateSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var path = GetSnapshotPath(windowId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, snapshot, cancellationToken: cancellationToken);
    }

    private string GetSnapshotPath(string windowId)
    {
        if (string.IsNullOrWhiteSpace(windowId))
        {
            throw new ArgumentException("windowId 不能为空。", nameof(windowId));
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var safeWindowId = new string(windowId.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return Path.Combine(_storageDirectory, safeWindowId + ".json");
    }
}
