using System;
using System.IO;

namespace BloodyMaryLauncher.Services;

public static class CacheService
{
    private static readonly string BasePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RedM", "RedM.app", "data");

    private static readonly string[] CacheFolders = { "server-cache", "server-cache-priv" };

    public static (bool success, string message) ClearCache()
    {
        try
        {
            int deletedCount = 0;

            foreach (var folder in CacheFolders)
            {
                var fullPath = Path.Combine(BasePath, folder);
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, recursive: true);
                    Directory.CreateDirectory(fullPath);
                    deletedCount++;
                }
            }

            return deletedCount > 0
                ? (true, $"Cache erfolgreich geleert ({deletedCount} Ordner bereinigt).")
                : (true, "Keine Cache-Ordner gefunden – nichts zu bereinigen.");
        }
        catch (Exception ex)
        {
            return (false, $"Fehler beim Leeren des Caches: {ex.Message}");
        }
    }
}
