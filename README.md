# BloodyMary Launcher

BloodyMary Launcher ist ein Windows-Launcher für RedM auf Basis von Avalonia. Das Projekt bündelt Server-Connect, Update-Kanal-Umschaltung, Cache-Reset, Grafik-Reset und optionales Launcher-Selbstupdate über GitHub Releases.

## Funktionen

- RedM.exe automatisch finden oder manüll auswählen
- Verbindung zu server.bloodymary.io starten
- Update-Kanäle production, beta, unstable und Grafik-Branch umschalten
- Cache und Grafikdaten zurücksetzen
- Optionales Launcher-Auto-Update über GitHub Releases

## Tech-Stack

- C#
- .NET 8
- Avalonia 11
- CommunityToolkit.Mvvm

## Projektstruktur

- BloodyMaryLauncher/Views enthält die Avalonia-Oberflächen
- BloodyMaryLauncher/ViewModels enthält die MVVM-Logik
- BloodyMaryLauncher/Services enthält RedM-, Update- und Dateisystem-Funktionalität
- BloodyMaryLauncher/Assets enthält Icons und UI-Ressourcen
- build.bat erstellt einen Release-Build nach publish/

## Voraussetzungen

- Windows
- .NET 8 SDK für Entwicklung und lokales Starten
- Optional ein GitHub-Repository mit Releases für das Launcher-Auto-Update

## Entwicklung

Restore und Build:

    dotnet restore BloodyMaryLauncher/BloodyMaryLauncher.csproj
    dotnet build BloodyMaryLauncher/BloodyMaryLauncher.csproj

Start im Debug:

    dotnet run --project BloodyMaryLauncher/BloodyMaryLauncher.csproj

Release-Publish:

    build.bat

Der Release-Build landet in publish/ und erzeugt eine self-contained Single-File-EXE für win-x64.

## Konfiguration

Die Konfiguration liegt zur Laufzeit unter %LocalAppData%/BloodyMaryLauncher/launcher_config.json.

Falls bei bestehenden Installationen noch eine launcher_config.json neben der EXE liegt, wird sie beim ersten Start weiterhin gelesen und automatisch in den neuen AppData-Ordner übernommen.

Wichtige Felder:

- RedMExePath: Optionaler fixer Pfad zu RedM.exe
- EnableLauncherAutoUpdate: Aktiviert die GitHub-Updateprüfung
- GitHubOwner: Owner des GitHub-Repositories
- GitHubRepo: Name des GitHub-Repositories
- GitHubAssetName: Erwarteter Dateiname des Release-Assets, standardmässig BloodyMaryLauncher.exe

Wenn GitHubOwner oder GitHubRepo leer sind, bleibt das Launcher-Auto-Update deaktiviert.

## Versionierung

Die .gitignore blendet Build-Artefakte, IDE-Dateien und lokale Tooling-Ordner aus, damit nur relevante Projektdateien versioniert werden.