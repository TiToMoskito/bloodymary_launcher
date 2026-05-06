using System;
using System.IO;

namespace BloodyMaryLauncher.Services;

public static class RedMPathService
{
    private static readonly string DefaultRedMPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RedM", "RedM.exe");

    public static string? FindRedMPath()
    {
        // 1) Check saved config
        var saved = LoadSavedPath();
        if (!string.IsNullOrEmpty(saved) && File.Exists(saved))
            return saved;

        // 2) Check default location
        if (File.Exists(DefaultRedMPath))
        {
            SavePath(DefaultRedMPath);
            return DefaultRedMPath;
        }

        return null;
    }

    public static void SavePath(string path)
    {
        LauncherConfigService.Update(config => config.RedMExePath = path);
    }

    private static string? LoadSavedPath()
    {
        return LauncherConfigService.Load().RedMExePath;
    }
}
