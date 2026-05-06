using System;
using System.IO;
using System.Linq;

namespace BloodyMaryLauncher.Services;

public static class UpdateChannelService
{
    private static readonly string IniPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RedM", "RedM.app", "CitizenFX.ini");

    public static string GetCurrentChannel()
    {
        if (!File.Exists(IniPath))
            return "production";

        var lines = File.ReadAllLines(IniPath);
        var channelLine = lines.FirstOrDefault(l =>
            l.TrimStart().StartsWith("UpdateChannel=", StringComparison.OrdinalIgnoreCase));

        if (channelLine == null)
            return "production";

        var value = channelLine.Split('=', 2).ElementAtOrDefault(1)?.Trim() ?? "production";
        return value;
    }

    public static (bool success, string message) SetChannel(string channel)
    {
        try
        {
            var dir = Path.GetDirectoryName(IniPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string[] lines;

            if (File.Exists(IniPath))
            {
                lines = File.ReadAllLines(IniPath);
                bool found = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith("UpdateChannel=", StringComparison.OrdinalIgnoreCase))
                    {
                        lines[i] = $"UpdateChannel={channel}";
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    lines = lines.Append($"UpdateChannel={channel}").ToArray();
                }
            }
            else
            {
                lines = new[] { $"UpdateChannel={channel}" };
            }

            File.WriteAllLines(IniPath, lines);
            return (true, $"Update-Channel auf '{channel}' gesetzt.");
        }
        catch (Exception ex)
        {
            return (false, $"Fehler beim Setzen des Channels: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps UI display name to INI value.
    /// </summary>
    public static string DisplayNameToChannel(string displayName) => displayName switch
    {
        "Release" => "production",
        "Beta" => "beta",
        "Untested" => "unstable",
        "Grafik Verbesserung" => "specific/feature/rdr3-graphic-improvements",
        _ => "production"
    };

    public static string ChannelToDisplayName(string channel) => channel switch
    {
        "production" => "Release",
        "beta" => "Beta",
        "unstable" => "Untested",
        "specific/feature/rdr3-graphic-improvements" => "Grafik Verbesserung",
        _ => "Release"
    };
}
