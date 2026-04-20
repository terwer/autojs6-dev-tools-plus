using System.Runtime.InteropServices;

namespace Infrastructure.Adb;

internal static class AdbExecutableLocator
{
    public static string? Resolve(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return ResolveExistingPath(explicitPath);
        }

        foreach (var candidate in EnumerateCandidates())
        {
            var resolved = ResolveExistingPath(candidate);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateCandidates()
    {
        var adbFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "adb.exe" : "adb";
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(pathEnv))
        {
            foreach (var path in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                candidates.Add(Path.Combine(path, adbFileName));
            }
        }

        var sdkRoots = new[]
        {
            Environment.GetEnvironmentVariable("ADB_PATH"),
            Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
            Environment.GetEnvironmentVariable("ANDROID_HOME"),
            Environment.GetEnvironmentVariable("ANDROID_SDK_HOME")
        };

        foreach (var sdkRoot in sdkRoots.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            candidates.Add(Path.Combine(sdkRoot!, "platform-tools", adbFileName));
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        foreach (var candidate in new[]
                 {
                     Path.Combine(localApplicationData, "Android", "Sdk", "platform-tools", adbFileName),
                     Path.Combine(programFiles, "Android", "Sdk", "platform-tools", adbFileName),
                     Path.Combine(home, "AppData", "Local", "Android", "Sdk", "platform-tools", adbFileName),
                     Path.Combine(home, "Library", "Android", "sdk", "platform-tools", adbFileName),
                     Path.Combine(home, "Android", "Sdk", "platform-tools", adbFileName),
                     Path.Combine("/opt/homebrew/bin", adbFileName),
                     Path.Combine("/usr/local/bin", adbFileName),
                     Path.Combine("/usr/bin", adbFileName)
                 })
        {
            candidates.Add(candidate);
        }

        return candidates;
    }

    private static string? ResolveExistingPath(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(candidate));
        return File.Exists(fullPath) ? fullPath : null;
    }
}
