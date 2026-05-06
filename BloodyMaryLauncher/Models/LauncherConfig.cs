namespace BloodyMaryLauncher.Models;

public sealed class LauncherConfig
{
    public string? RedMExePath { get; set; }
    public bool EnableLauncherAutoUpdate { get; set; } = true;
    public string? GitHubOwner { get; set; } = "TiToMoskito";
    public string? GitHubRepo { get; set; } = "bloodymary_launcher";
    public string? GitHubAssetName { get; set; } = "BloodyMaryLauncher.exe";
}