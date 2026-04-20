namespace Core.Models.Desktop;

public sealed record UserMessage(UserMessageSeverity Severity, string Title, string Message);
