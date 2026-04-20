namespace Core.Models.Desktop;

public sealed record SaveFileRequest(
    string Title,
    string SuggestedFileName,
    string DefaultExtension,
    IReadOnlyList<FileDialogFilter> Filters,
    string? InitialDirectory = null);
