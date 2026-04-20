using Core.Models.Desktop;

namespace Core.Abstractions.Desktop;

public interface IFileSaveDialogService
{
    Task<SaveFileResult> SaveFileAsync(SaveFileRequest request, CancellationToken cancellationToken = default);
}
