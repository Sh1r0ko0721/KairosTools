# release.ps1 — 全自动：编译 + 推送源码 + 打 tag + 创建 Release + 上传 exe
# 用法：
#   .\release.ps1 1.0.1
#   .\release.ps1 1.0.1 "update notes"

param(
    [Parameter(Mandatory=$true)][string]$Version,
    [Parameter(Mandatory=$false)][string]$Notes = "Edit this description"
)

$ErrorActionPreference = "Continue"
$tag = "v$Version"
$repo = "Sh1r0ko0721/KairosTools"
$token = $env:GITHUB_TOKEN

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host "GITHUB_TOKEN 环境变量未读取到" -ForegroundColor Red
    Write-Host "请检查：" -ForegroundColor Yellow
    Write-Host "  1. 是否跑过 setx GITHUB_TOKEN ghp_xxxx" -ForegroundColor White
    Write-Host "  2. 是否重启过 PowerShell（setx 后必须重启）" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "  Release $tag" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# 1. 编译
Write-Host "[1/4] 编译..." -ForegroundColor Yellow
Push-Location KairosTools
dotnet publish -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "编译失败" -ForegroundColor Red
    Pop-Location
    exit 1
}
Pop-Location

$exePath = "KairosTools\bin\Release\net10.0-windows\win-x64\publish\KairosTools.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "找不到 exe：$exePath" -ForegroundColor Red
    exit 1
}
$exeFull = (Resolve-Path $exePath).Path
Write-Host "      OK：$exeFull" -ForegroundColor Green

# 2. 提交源码 + 打 tag + 推送
Write-Host "[2/4] 提交源码 + 推送 tag..." -ForegroundColor Yellow
git add . 2>&1 | Out-Null
git commit -m "release: $tag" 2>&1 | Out-Null
git push 2>&1 | Out-Null
git tag $tag 2>&1 | Out-Null
git push origin $tag 2>&1 | Out-Null
Write-Host "      OK" -ForegroundColor Green

# 3. 创建 Release
Write-Host "[3/4] 创建 Release..." -ForegroundColor Yellow

$headers = @{
    "Authorization" = "token $token"
    "Accept"        = "application/vnd.github+json"
    "User-Agent"    = "KairosTools-Release"
}

$releaseBody = @{
    tag_name   = $tag
    name       = $tag
    body       = $Notes
    draft      = $false
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
    Write-Host "创建 Release 失败：$_" -ForegroundColor Red
    exit 1
}
Write-Host "      OK" -ForegroundColor Green

# 4. 上传 exe
Write-Host "[4/4] 上传 exe..." -ForegroundColor Yellow

# ★ 关键修正：用 Split('{')[0] 取出 upload_url 的前半部分
$baseUrl = $release.upload_url.Split('{')[0]
$uploadUrl = "${baseUrl}?name=KairosTools.exe"

try {
    Invoke-RestMethod `
        -Uri $uploadUrl `
        -Method Post `
        -Headers $headers `
        -InFile $exeFull `
        -ContentType "application/octet-stream" | Out-Null
} catch {
    Write-Host "上传 exe 失败：$_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "  完成！" -ForegroundColor Green
Write-Host "  https://github.com/$repo/releases/tag/$tag" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Green
Write-Host ""