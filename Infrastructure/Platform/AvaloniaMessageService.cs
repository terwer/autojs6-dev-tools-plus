using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Core.Abstractions.Desktop;
using Core.Models.Desktop;

namespace Infrastructure.Platform;

/// <summary>
/// 基于 Avalonia WindowNotificationManager 的提示消息实现。
/// </summary>
public sealed class AvaloniaMessageService : IMessageService
{
    private readonly Func<TopLevel?> _topLevelAccessor;
    private TopLevel? _currentHost;
    private WindowNotificationManager? _notificationManager;

    public AvaloniaMessageService(Func<TopLevel?> topLevelAccessor)
    {
        _topLevelAccessor = topLevelAccessor ?? throw new ArgumentNullException(nameof(topLevelAccessor));
    }

    public Task ShowAsync(UserMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        var topLevel = _topLevelAccessor()
                       ?? throw new InvalidOperationException("当前未找到可用的 Avalonia TopLevel。");

        if (!ReferenceEquals(_currentHost, topLevel) || _notificationManager is null)
        {
            _currentHost = topLevel;
            _notificationManager = new WindowNotificationManager(topLevel)
            {
                Position = NotificationPosition.BottomRight,
                MaxItems = 5
            };
        }

        _notificationManager.Show(new Notification(
            message.Title,
            message.Message,
            MapNotificationType(message.Severity),
            MapExpiration(message.Severity)));

        return Task.CompletedTask;
    }

    private static NotificationType MapNotificationType(UserMessageSeverity severity)
    {
        return severity switch
        {
            UserMessageSeverity.Success => NotificationType.Success,
            UserMessageSeverity.Warning => NotificationType.Warning,
            UserMessageSeverity.Error => NotificationType.Error,
            _ => NotificationType.Information
        };
    }

    private static TimeSpan MapExpiration(UserMessageSeverity severity)
    {
        return severity == UserMessageSeverity.Error
            ? TimeSpan.FromSeconds(5)
            : TimeSpan.FromSeconds(3);
    }
}
