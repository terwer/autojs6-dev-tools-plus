namespace App.Avalonia.Services;

public interface IWorkbenchFilePickerService
{
    Task<string?> PickImageFileAsync(string title, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> PickImageFilesAsync(string title, CancellationToken cancellationToken = default);
}
