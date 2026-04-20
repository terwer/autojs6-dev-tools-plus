using Core.Models.Desktop;

namespace Core.Abstractions.Desktop;

public interface IMessageService
{
    Task ShowAsync(UserMessage message, CancellationToken cancellationToken = default);
}
