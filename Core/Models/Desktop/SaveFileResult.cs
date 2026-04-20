namespace Core.Models.Desktop;

public sealed record SaveFileResult(bool Confirmed, string? FilePath)
{
    public static SaveFileResult Cancelled { get; } = new(false, null);
}
