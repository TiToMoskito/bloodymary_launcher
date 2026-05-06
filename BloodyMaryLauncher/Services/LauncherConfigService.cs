using System;
using System.IO;
using System.Text.Json;
using BloodyMaryLauncher.Models;

namespace BloodyMaryLauncher.Services;

public static class LauncherConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string ConfigDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BloodyMaryLauncher");

    private static string LegacyConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher_config.json");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "launcher_config.json");

    private static void DeleteLegacyConfigIfPresent()
    {
        if (!File.Exists(LegacyConfigPath))
            return;

        try
        {
            File.Delete(LegacyConfigPath);
        }
        catch
        {
        }
    }

    public static LauncherConfig Load()
    {
        var configPath = File.Exists(ConfigPath) ? ConfigPath : LegacyConfigPath;

        if (!File.Exists(configPath))
            return new LauncherConfig();

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<LauncherConfig>(json) ?? new LauncherConfig();

            if (configPath == LegacyConfigPath && !File.Exists(ConfigPath))
                Save(config);

            if (File.Exists(ConfigPath))
                DeleteLegacyConfigIfPresent();

            return config;
        }
        catch
        {
            return new LauncherConfig();
        }
    }

    public static void Save(LauncherConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    public static void Update(Action<LauncherConfig> updateAction)
    {
        var config = Load();
        updateAction(config);
        Save(config);
    }
}