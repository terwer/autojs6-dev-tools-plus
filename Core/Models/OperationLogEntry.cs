namespace Core.Models;

/// <summary>
/// 操作日志项。
/// </summary>
public sealed record OperationLogEntry(
    DateTimeOffset Timestamp,
    string Category,
    OperationLogLevel Level,
    string Message,
    long? ElapsedMilliseconds = null);
