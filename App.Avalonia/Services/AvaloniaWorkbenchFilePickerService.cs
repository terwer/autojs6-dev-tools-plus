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
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("图像文件")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"]
                }
            ]
        });

        var file = files.FirstOrDefault();
        if (file is null)
        {
            return null;
        }

        return file.Path.IsAbsoluteUri ? file.Path.LocalPath : file.Path.ToString();
    }
}
