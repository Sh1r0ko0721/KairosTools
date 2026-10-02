# release.ps1
# Usage:
#   .\release.ps1 1.0.1
#   .\release.ps1 1.0.1 "update notes"

param(
    [Parameter(Mandatory=$true)][string]$Version,
    [Parameter(Mandatory=$false)][string]$Notes = "Edit this description"
)

$ErrorActionPreference = "Stop"
$tag = "v$Version"
$repo = "Sh1r0ko0721/KairosTools"
$amp = [char]38

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "  Release $tag" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# 1. build
Write-Host "[1/3] Building..." -ForegroundColor Yellow
Push-Location KairosTools
dotnet publish -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

$exePath = "KairosTools\bin\Release\net10.0-windows\win-x64\publish\KairosTools.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "exe not found: $exePath" -ForegroundColor Red
    exit 1
}
$exeFull = (Resolve-Path $exePath).Path
Write-Host "      exe: $exeFull" -ForegroundColor Green

# 2. commit + tag + push
Write-Host "[2/3] Committing and pushing tag..." -ForegroundColor Yellow
git add .
git commit -m "release: $tag"
if ($LASTEXITCODE -ne 0) {
    Write-Host "      (no changes to commit)" -ForegroundColor DarkGray
}
git push
git tag $tag 2>$null
git push origin $tag 2>$null

# 3. open release page
Write-Host "[3/3] Opening GitHub release page..." -ForegroundColor Yellow
$body = [uri]::EscapeDataString($Notes)
$url = "https://github.com/${repo}/releases/new?tag=${tag}${amp}title=${tag}${amp}body=${body}"
Start-Process $url

Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "  Done!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""
Write-Host "Now do these in browser:" -ForegroundColor Cyan
Write-Host "  1. Edit the description" -ForegroundColor White
Write-Host "  2. Drag this exe into the attachment box:" -ForegroundColor White
Write-Host "     $exeFull" -ForegroundColor Yellow
Write-Host "  3. Click Publish release" -ForegroundColor White
Write-Host ""