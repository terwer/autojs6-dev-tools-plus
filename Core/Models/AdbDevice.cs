namespace Core.Models;

/// <summary>
/// ADB 设备信息。
/// </summary>
public sealed class AdbDevice
{
    /// <summary>
    /// 设备序列号。
    /// </summary>
    public required string Serial { get; init; }

    /// <summary>
    /// 设备型号。
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// 设备状态，例如 device、offline、unauthorized。
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// 连接类型，例如 usb、tcpip。
    /// </summary>
    public string? ConnectionType { get; init; }

    /// <summary>
    /// 产品名称。
    /// </summary>
    public string? Product { get; init; }

    /// <summary>
    /// 传输 ID。
    /// </summary>
    public string? TransportId { get; init; }

    /// <summary>
    /// 是否为在线设备。
    /// </summary>
    public bool IsOnline =>
        string.Equals(State, "device", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(State, "online", StringComparison.OrdinalIgnoreCase);
}
