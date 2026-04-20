using Core.Models;

namespace Core.Abstractions;

/// <summary>
/// 基于 AdvancedSharpAdbClient 的 ADB 服务抽象。
/// </summary>
public interface IAdbService
{
    event EventHandler<OperationLogEntry>? OperationLogged;

    Task<IReadOnlyList<AdbDevice>> ScanDevicesAsync(CancellationToken cancellationToken = default);

    Task<AdbScreenshot> CaptureScreenshotAsync(AdbDevice device, CancellationToken cancellationToken = default);

    Task<string> DumpUiHierarchyAsync(AdbDevice device, CancellationToken cancellationToken = default);

    Task<bool> IsDeviceOnlineAsync(AdbDevice device, CancellationToken cancellationToken = default);

    Task<string> ConnectDeviceAsync(string host, int port, CancellationToken cancellationToken = default);
}
