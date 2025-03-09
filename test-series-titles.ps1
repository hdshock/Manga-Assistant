# Test script to verify series titles are being displayed correctly
Write-Host "Testing series title display in Manga Assistant"
Write-Host "==============================================="

# Get the path to the executable
$exePath = Join-Path $PSScriptRoot "src\MangaAssistant.WPF\bin\Debug\net7.0-windows\MangaAssistant.WPF.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "Error: Executable not found at $exePath" -ForegroundColor Red
    Write-Host "Please build the project first."
    exit 1
}

Write-Host "Starting Manga Assistant application..."
Write-Host "Please verify that series titles are displayed correctly on posters in the library."
Write-Host "The titles should follow this priority:"
Write-Host "1. Metadata file in the series folder (series-info.json)"
Write-Host "2. Series title from CBZ files' ComicInfo.xml"
Write-Host "3. Folder name if no other titles are available"
Write-Host ""
Write-Host "Press Enter to launch the application..."
$null = Read-Host

# Start the application
Start-Process $exePath
