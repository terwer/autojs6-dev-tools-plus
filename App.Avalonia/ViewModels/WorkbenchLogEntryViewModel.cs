namespace App.Avalonia.ViewModels;

public sealed class WorkbenchLogEntryViewModel
{
    public required string TimeText { get; init; }

    public required string Category { get; init; }

    public required string Message { get; init; }
}
