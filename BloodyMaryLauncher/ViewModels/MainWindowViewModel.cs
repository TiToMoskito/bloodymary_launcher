using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BloodyMaryLauncher.Services;

namespace BloodyMaryLauncher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusMessage = "Bereit.";

    [ObservableProperty]
    private bool _isReleaseActive;

    [ObservableProperty]
    private bool _isBetaActive;

    [ObservableProperty]
    private bool _isUntestedActive;

    [ObservableProperty]
    private bool _isGrafikActive;

    [ObservableProperty]
    private string _redMPath = string.Empty;

    [ObservableProperty]
    private bool _isRedMFound;

    public MainWindowViewModel()
    {
        LoadRedMPath();
        LoadCurrentChannel();
        _ = CheckForLauncherUpdatesAsync();
    }

    private void LoadRedMPath()
    {
        var path = RedMPathService.FindRedMPath();
        if (path != null)
        {
            RedMPath = path;
            IsRedMFound = true;
        }
        else
        {
            RedMPath = string.Empty;
            IsRedMFound = false;
            StatusMessage = "RedM.exe nicht gefunden – bitte manuell auswählen.";
        }
    }

    private void LoadCurrentChannel()
    {
        var channel = UpdateChannelService.GetCurrentChannel();
        SetChannelToggle(channel);
    }

    private async Task CheckForLauncherUpdatesAsync()
    {
        try
        {
            var updateCheck = await AppUpdateService.CheckForUpdateAsync();
            if (!updateCheck.IsEnabled || !updateCheck.IsUpdateAvailable)
            {
                SetBackgroundStatus(updateCheck.Message);
                return;
            }

            StatusMessage = $"Launcher-Update {updateCheck.AvailableVersion} gefunden – Download startet...";

            var preparedUpdate = await AppUpdateService.DownloadAndPrepareUpdateAsync(updateCheck);
            if (!preparedUpdate.IsSuccess)
            {
                SetBackgroundStatus(preparedUpdate.Message);
                return;
            }

            StatusMessage = $"Launcher wird auf Version {updateCheck.AvailableVersion} aktualisiert...";

            if (!AppUpdateService.StartPreparedUpdate(preparedUpdate))
            {
                SetBackgroundStatus("Update konnte nicht gestartet werden.");
                return;
            }

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
        catch (Exception ex)
        {
            SetBackgroundStatus($"Updateprüfung fehlgeschlagen: {ex.Message}");
        }
    }

    private void SetChannelToggle(string channel)
    {
        IsReleaseActive = channel == "production";
        IsBetaActive = channel == "beta";
        IsUntestedActive = channel == "unstable";
        IsGrafikActive = channel == "specific/feature/rdr3-graphic-improvements";
    }

    [RelayCommand]
    private async Task BrowseRedMPath()
    {
        var window = GetMainWindow();
        if (window == null) return;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "RedM.exe auswählen",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("RedM Executable") { Patterns = new[] { "RedM.exe" } },
                new("Alle Dateien") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            RedMPathService.SavePath(path);
            RedMPath = path;
            IsRedMFound = true;
            StatusMessage = $"RedM-Pfad gesetzt: {path}";
        }
    }

    [RelayCommand]
    private void Connect()
    {
        if (!IsRedMFound || string.IsNullOrEmpty(RedMPath))
        {
            StatusMessage = "RedM.exe nicht gefunden – bitte zuerst den Pfad auswählen.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = RedMPath,
                Arguments = "-pure_1 +connect server.bloodymary.io",
                UseShellExecute = true
            });
            StatusMessage = "RedM wird gestartet...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler beim Starten: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearCache()
    {
        var (success, message) = CacheService.ClearCache();
        StatusMessage = message;
    }

    [RelayCommand]
    private void SetChannelRelease()
    {
        var (success, message) = UpdateChannelService.SetChannel("production");
        if (success) SetChannelToggle("production");
        StatusMessage = message;
    }

    [RelayCommand]
    private void SetChannelBeta()
    {
        var (success, message) = UpdateChannelService.SetChannel("beta");
        if (success) SetChannelToggle("beta");
        StatusMessage = message;
    }

    [RelayCommand]
    private void SetChannelUntested()
    {
        var (success, message) = UpdateChannelService.SetChannel("unstable");
        if (success) SetChannelToggle("unstable");
        StatusMessage = message;
    }

    [RelayCommand]
    private void SetChannelGrafik()
    {
        var (success, message) = UpdateChannelService.SetChannel("specific/feature/rdr3-graphic-improvements");
        if (success) SetChannelToggle("specific/feature/rdr3-graphic-improvements");
        StatusMessage = message;
    }

    [RelayCommand]
    private void ResetGraphics()
    {
        var (success, message) = GraphicsResetService.ResetGraphics();
        StatusMessage = message;
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    private void SetBackgroundStatus(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (string.Equals(StatusMessage, "Bereit.", StringComparison.Ordinal))
        {
            StatusMessage = message;
        }
    }
}
