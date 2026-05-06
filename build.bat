@echo off
echo ========================================
echo   BloodyMary Launcher - Build
echo ========================================
echo.

if exist publish (
    echo Bereinige alten Build...
    rmdir /s /q publish
)

echo Erstelle portable EXE...
echo.

dotnet publish BloodyMaryLauncher\BloodyMaryLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o publish

if %errorlevel% neq 0 (
    echo.
    echo FEHLER beim Build!
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Build erfolgreich!
echo   Output: publish\BloodyMaryLauncher.exe
echo ========================================
pause
