# release.ps1
# Usage:
#   .\release.ps1 1.0.1
#   .\release.ps1 1.0.1 "update notes"

param(
    [Parameter(Mandatory=$true)][string]$Version,
    [Parameter(Mandatory=$false)][string]$Notes = "Edit this description"
)

$ErrorActionPreference = "Continue"
$tag = "v$Version"
$repo = "Sh1r0ko0721/KairosTools"
$token = $env:GITHUB_TOKEN   # 从环境变量读，避免硬编码到脚本

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host "Please set GITHUB_TOKEN environment variable first" -ForegroundColor Red
    Write-Host "  setx GITHUB_TOKEN ghp_xxxxxxxxxxxx" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "  Release $tag" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# 1. build
Write-Host "[1/4] Building..." -ForegroundColor Yellow
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
Write-Host "[2/4] Committing and pushing tag..." -ForegroundColor Yellow
git add . 2>&1 | Out-Null
git commit -m "release: $tag" 2>&1 | Out-Null
git push 2>&1 | Out-Null
git tag $tag 2>&1 | Out-Null
git push origin $tag 2>&1 | Out-Null
Write-Host "      done" -ForegroundColor Green

# 3. create release via GitHub API
Write-Host "[3/4] Creating release..." -ForegroundColor Yellow
$headers = @{
    "Authorization" = "token $token"
    "Accept" = "application/vnd.github+json"
    "User-Agent" = "KairosTools-Release-Script"
}

$releaseBody = @{
    tag_name = $tag
    name = $tag
    body = $Notes
    draft = $false
    prerelease = $false
} | ConvertTo-Json

try {
    $release = Invoke-RestMethod `
        -Uri "https://api.github.com/repos/$repo/releases" `
        -Method Post `
        -Headers $headers `
        -Body $releaseBody `
        -ContentType "application/json"
} catch {
    Write-Host "Failed to create release: $_" -ForegroundColor Red
    exit 1
}

Write-Host "      release created" -ForegroundColor Green

# 4. upload exe asset
Write-Host "[4/4] Uploading exe..." -ForegroundColor Yellow
$uploadUrl = $release.upload_url -replace '\{.*\}', ''
$uploadUrl = "$uploadUrl?name=KairosTools.exe"

try {
    Invoke-RestMethod `
        -Uri $uploadUrl `
        -Method Post `
        -Headers $headers `
        -InFile $exeFull `
        -ContentType "application/octet-stream" | Out-Null
} catch {
    Write-Host "Failed to upload exe: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "  Done!" -ForegroundColor Green
Write-Host "  https://github.com/$repo/releases/tag/$tag" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Green
Write-Host ""