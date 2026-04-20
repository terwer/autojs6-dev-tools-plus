using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Core.Abstractions.Desktop;
using Core.Models.Desktop;

namespace Infrastructure.Platform;

/// <summary>
/// 基于 Avalonia StorageProvider 的保存对话框实现。
/// </summary>
public sealed class AvaloniaFileSaveDialogService : IFileSaveDialogService
{
    private readonly Func<TopLevel?> _topLevelAccessor;

    public AvaloniaFileSaveDialogService(Func<TopLevel?> topLevelAccessor)
    {
        _topLevelAccessor = topLevelAccessor ?? throw new ArgumentNullException(nameof(topLevelAccessor));
    }

    public async Task<SaveFileResult> SaveFileAsync(SaveFileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var topLevel = _topLevelAccessor()
                       ?? throw new InvalidOperationException("当前未找到可用的 Avalonia TopLevel。");

        if (!topLevel.StorageProvider.CanSave)
        {
            throw new InvalidOperationException("当前平台不支持文件保存对话框。");
        }

        IStorageFolder? startLocation = null;
        if (!string.IsNullOrWhiteSpace(request.InitialDirectory) && Directory.Exists(request.InitialDirectory))
        {
            var folderUri = new Uri(Path.GetFullPath(request.InitialDirectory));
            startLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(folderUri);
        }

        var fileTypes = request.Filters.Select(filter => new FilePickerFileType(filter.Name)
        {
            Patterns = filter.Patterns
        }).ToArray();

        var options = new FilePickerSaveOptions
        {
            Title = request.Title,
            SuggestedFileName = request.SuggestedFileName,
            SuggestedStartLocation = startLocation,
            DefaultExtension = NormalizeExtension(request.DefaultExtension),
            ShowOverwritePrompt = true,
            FileTypeChoices = fileTypes,
            SuggestedFileType = fileTypes.FirstOrDefault()
        };

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);
        if (file is null)
        {
            return SaveFileResult.Cancelled;
        }

        var filePath = file.Path.IsAbsoluteUri ? file.Path.LocalPath : file.Path.ToString();
        return new SaveFileResult(true, filePath);
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        return extension.StartsWith(".", StringComparison.Ordinal) ? extension : "." + extension;
    }
}
