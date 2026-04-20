using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace App.Avalonia.Services;

public sealed class AvaloniaWorkbenchFilePickerService : IWorkbenchFilePickerService
{
    private readonly Func<TopLevel?> _topLevelAccessor;

    public AvaloniaWorkbenchFilePickerService(Func<TopLevel?> topLevelAccessor)
    {
        _topLevelAccessor = topLevelAccessor ?? throw new ArgumentNullException(nameof(topLevelAccessor));
    }

    public async Task<string?> PickImageFileAsync(string title, CancellationToken cancellationToken = default)
    {
        var files = await PickImageFilesInternalAsync(title, false, cancellationToken);
        return files.FirstOrDefault();
    }

    public async Task<IReadOnlyList<string>> PickImageFilesAsync(string title, CancellationToken cancellationToken = default)
    {
        return await PickImageFilesInternalAsync(title, true, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> PickImageFilesInternalAsync(string title, bool allowMultiple, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var topLevel = _topLevelAccessor()
                       ?? throw new InvalidOperationException("当前未找到可用的 Avalonia TopLevel。");

        if (!topLevel.StorageProvider.CanOpen)
        {
            throw new InvalidOperationException("当前平台不支持文件打开对话框。");
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple,
            FileTypeFilter =
            [
                new FilePickerFileType("图像文件")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"]
                }
            ]
        });

        return files
            .Select(file => file.Path.IsAbsoluteUri ? file.Path.LocalPath : file.Path.ToString())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
    }
}
