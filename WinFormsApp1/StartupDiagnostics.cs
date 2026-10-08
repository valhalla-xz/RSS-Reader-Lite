namespace WinFormsApp1;

internal static class StartupDiagnostics
{
    private static readonly object Gate = new();
    public static string LogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LocalNewsReader", "startup.log");

    public static void Write(Exception exception, string source)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath)!;
            Directory.CreateDirectory(directory);
            lock (Gate)
                File.AppendAllText(LogPath, $"[{DateTimeOffset.Now:O}] {source}{Environment.NewLine}{exception}{Environment.NewLine}{new string('-', 72)}{Environment.NewLine}");
        }
        catch { }
    }
}
