namespace Core.Models.Desktop;

public sealed record WindowStateSnapshot(
    double Width,
    double Height,
    double X,
    double Y,
    DesktopWindowState State);
