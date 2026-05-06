using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BloodyMaryLauncher.Models;

namespace BloodyMaryLauncher.Services;

public static class AppUpdateService
{
    private static readonly HttpClient HttpClient = CreateHttpClient();
    private static readonly Regex VersionRegex = new(@"\d+(\.\d+){0,3}", RegexOptions.Compiled);

    public static async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        var config = LauncherConfigService.Load();
        var currentVersion = GetCurrentVersion();

        if (!config.EnableLauncherAutoUpdate)
        {
            return UpdateCheckResult.Disabled(currentVersion);
        }

        if (!HasGitHubConfiguration(config))
        {
            return UpdateCheckResult.Disabled(currentVersion);
        }

        if (!TryResolveRepository(config, out var githubOwner, out var githubRepo))
        {
            return UpdateCheckResult.Failed(
                currentVersion,
                "GitHub-Konfiguration ist ungültig. Verwende GitHubOwner + GitHubRepo, owner/repo oder eine GitHub-URL.");
        }

        using var response = await HttpClient.GetAsync(
            $"repos/{githubOwner}/{githubRepo}/releases/latest",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = response.StatusCode == HttpStatusCode.NotFound
                ? $"GitHub-Release nicht gefunden. Prüfe {githubOwner}/{githubRepo} und ob unter Releases eine aktuelle Version veröffentlicht ist."
                : $"GitHub-Updateprüfung fehlgeschlagen ({(int)response.StatusCode}).";

            return UpdateCheckResult.Failed(
                currentVersion,
                message);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(
            stream,
            cancellationToken: cancellationToken);

        if (release == null)
        {
            return UpdateCheckResult.Failed(currentVersion, "GitHub-Antwort konnte nicht gelesen werden.");
        }

        var latestVersion = NormalizeVersion(release.TagName);
        if (!IsNewerVersion(currentVersion, latestVersion))
        {
            return UpdateCheckResult.UpToDate(currentVersion, latestVersion);
        }

        var asset = SelectAsset(release, config.GitHubAssetName);
        if (asset == null)
        {
            return UpdateCheckResult.Failed(
                currentVersion,
                "Es wurde kein passendes Release-Asset auf GitHub gefunden.");
        }

        return UpdateCheckResult.Available(currentVersion, latestVersion, asset);
    }

    public static async Task<PreparedUpdateResult> DownloadAndPrepareUpdateAsync(
        UpdateCheckResult update,
        CancellationToken cancellationToken = default)
    {
        if (!update.IsUpdateAvailable || update.Asset == null)
        {
            return PreparedUpdateResult.Failed("Es ist kein Update zum Herunterladen vorhanden.");
        }

        var workingDirectory = Path.Combine(
            Path.GetTempPath(),
            "BloodyMaryLauncher",
            "updates",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(workingDirectory);

        var assetPath = Path.Combine(workingDirectory, update.Asset.Name);
        using var response = await HttpClient.GetAsync(
            update.Asset.BrowserDownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return PreparedUpdateResult.Failed(
                $"Download des Updates fehlgeschlagen ({(int)response.StatusCode}).");
        }

        await using (var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var targetStream = File.Create(assetPath))
        {
            await sourceStream.CopyToAsync(targetStream, cancellationToken);
        }

        var replacementExecutable = ResolveReplacementExecutable(assetPath, workingDirectory);
        if (replacementExecutable == null)
        {
            return PreparedUpdateResult.Failed(
                "Das heruntergeladene Release enthält keine ersetzbare Launcher-Datei.");
        }

        return PreparedUpdateResult.Success(workingDirectory, replacementExecutable);
    }

    public static bool StartPreparedUpdate(PreparedUpdateResult preparedUpdate)
    {
        if (!preparedUpdate.IsSuccess
            || string.IsNullOrWhiteSpace(preparedUpdate.WorkingDirectory)
            || string.IsNullOrWhiteSpace(preparedUpdate.ReplacementExecutablePath))
        {
            return false;
        }

        var currentExecutable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExecutable))
        {
            return false;
        }

        var updateScriptPath = Path.Combine(preparedUpdate.WorkingDirectory, "apply_update.cmd");
        var script = $@"@echo off
setlocal
set ""SOURCE_EXE={preparedUpdate.ReplacementExecutablePath}""
set ""TARGET_EXE={currentExecutable}""

for /L %%i in (1,1,20) do (
    copy /Y ""%SOURCE_EXE%"" ""%TARGET_EXE%"" >nul 2>nul && goto start_app
    ping 127.0.0.1 -n 2 >nul
)

exit /b 1

:start_app
start """" ""%TARGET_EXE%""
exit /b 0
";

        File.WriteAllText(updateScriptPath, script);

        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"\"{updateScriptPath}\"\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = preparedUpdate.WorkingDirectory
        });

        return true;
    }

    public static string GetCurrentVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0, 0);
        return NormalizeVersion(version.ToString());
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("BloodyMaryLauncher-Updater");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private static bool HasGitHubConfiguration(LauncherConfig config)
    {
        return !string.IsNullOrWhiteSpace(config.GitHubOwner)
               || !string.IsNullOrWhiteSpace(config.GitHubRepo);
    }

    private static bool TryResolveRepository(LauncherConfig config, out string owner, out string repo)
    {
        owner = string.Empty;
        repo = string.Empty;

        var configuredOwner = config.GitHubOwner?.Trim();
        var configuredRepo = config.GitHubRepo?.Trim();

        if (!string.IsNullOrWhiteSpace(configuredRepo)
            && Uri.TryCreate(configuredRepo, UriKind.Absolute, out var repositoryUri)
            && repositoryUri.Host.EndsWith("github.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = repositoryUri.AbsolutePath
                .Trim('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length >= 2)
            {
                owner = segments[0];
                repo = NormalizeRepositoryName(segments[1]);
                return !string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo);
            }
        }

        if (!string.IsNullOrWhiteSpace(configuredRepo) && configuredRepo.Contains('/', StringComparison.Ordinal))
        {
            var segments = configuredRepo.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 2)
            {
                owner = segments[0];
                repo = NormalizeRepositoryName(segments[1]);
                return !string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo);
            }
        }

        if (string.IsNullOrWhiteSpace(configuredOwner) || string.IsNullOrWhiteSpace(configuredRepo))
        {
            return false;
        }

        owner = configuredOwner;
        repo = NormalizeRepositoryName(configuredRepo);
        return !string.IsNullOrWhiteSpace(repo);
    }

    private static GitHubAsset? SelectAsset(GitHubRelease release, string? configuredAssetName)
    {
        if (!string.IsNullOrWhiteSpace(configuredAssetName))
        {
            var configuredAsset = release.Assets.FirstOrDefault(asset =>
                string.Equals(asset.Name, configuredAssetName, StringComparison.OrdinalIgnoreCase));

            if (configuredAsset != null)
            {
                return configuredAsset;
            }
        }

        var executableName = Path.GetFileName(Environment.ProcessPath) ?? "BloodyMaryLauncher.exe";

        return release.Assets.FirstOrDefault(asset =>
                   string.Equals(asset.Name, executableName, StringComparison.OrdinalIgnoreCase))
               ?? release.Assets.FirstOrDefault(asset =>
                   asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveReplacementExecutable(string assetPath, string workingDirectory)
    {
        if (assetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return assetPath;
        }

        if (!assetPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var extractDirectory = Path.Combine(workingDirectory, "extracted");
        ZipFile.ExtractToDirectory(assetPath, extractDirectory, overwriteFiles: true);

        var executableName = Path.GetFileName(Environment.ProcessPath) ?? "BloodyMaryLauncher.exe";
        return Directory.EnumerateFiles(
                extractDirectory,
                executableName,
                SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    private static bool IsNewerVersion(string currentVersionText, string latestVersionText)
    {
        var currentVersion = ParseVersion(currentVersionText);
        var latestVersion = ParseVersion(latestVersionText);

        return latestVersion > currentVersion;
    }

    private static Version ParseVersion(string versionText)
    {
        return Version.TryParse(NormalizeVersion(versionText), out var version)
            ? version
            : new Version(0, 0, 0, 0);
    }

    private static string NormalizeVersion(string? versionText)
    {
        if (string.IsNullOrWhiteSpace(versionText))
        {
            return "0.0.0";
        }

        var match = VersionRegex.Match(versionText);
        return match.Success ? match.Value : "0.0.0";
    }

    private static string NormalizeRepositoryName(string repositoryName)
    {
        var normalizedRepositoryName = repositoryName.Trim().Trim('/');

        return normalizedRepositoryName.EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? normalizedRepositoryName[..^4]
            : normalizedRepositoryName;
    }

    public sealed record UpdateCheckResult(
        bool IsEnabled,
        bool IsUpdateAvailable,
        string CurrentVersion,
        string AvailableVersion,
        GitHubAsset? Asset,
        string? Message)
    {
        public static UpdateCheckResult Disabled(string currentVersion) =>
            new(false, false, currentVersion, currentVersion, null, null);

        public static UpdateCheckResult UpToDate(string currentVersion, string latestVersion) =>
            new(true, false, currentVersion, latestVersion, null, null);

        public static UpdateCheckResult Available(
            string currentVersion,
            string latestVersion,
            GitHubAsset asset) =>
            new(true, true, currentVersion, latestVersion, asset, null);

        public static UpdateCheckResult Failed(string currentVersion, string message) =>
            new(true, false, currentVersion, currentVersion, null, message);
    }

    public sealed record PreparedUpdateResult(
        bool IsSuccess,
        string? WorkingDirectory,
        string? ReplacementExecutablePath,
        string? Message)
    {
        public static PreparedUpdateResult Success(string workingDirectory, string replacementExecutablePath) =>
            new(true, workingDirectory, replacementExecutablePath, null);

        public static PreparedUpdateResult Failed(string message) =>
            new(false, null, null, message);
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public GitHubAsset[] Assets { get; set; } = Array.Empty<GitHubAsset>();
    }

    public sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}