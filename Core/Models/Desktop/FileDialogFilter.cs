namespace Core.Models.Desktop;

public sealed record FileDialogFilter(string Name, IReadOnlyList<string> Patterns);
