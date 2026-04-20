namespace App.Avalonia.Services;

public interface IWorkbenchFilePickerService
{
    Task<string?> PickImageFileAsync(string title, CancellationToken cancellationToken = default);
}
