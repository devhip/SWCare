# Builds everything the server needs into one folder (docs/Deploy.md). Run on the developer machine:
#
#   .\scripts\publish.ps1            # -> dist\SWCare
#   .\scripts\publish.ps1 -Zip       # -> also dist\SWCare-<date>.zip, ready to copy to the server
#
# Contents: api\ (IIS site), web\ (static site), migrate.sql (idempotent), tools\SheetImport\, deploy-iis.ps1, backup.ps1
param(
    [string]$Output = "",
    [switch]$Zip,
    # npm ci = clean install from package-lock.json (slower); default reuses web\node_modules when present
    [switch]$NpmCi
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
if (-not $Output) { $Output = Join-Path $RepoRoot "dist\SWCare" }

function Invoke-Step([string]$Title, [scriptblock]$Action) {
    Write-Host "==> $Title" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) { throw "$Title failed (exit $LASTEXITCODE)" }
}

if (Test-Path $Output) { Remove-Item $Output -Recurse -Force }
New-Item -ItemType Directory -Force $Output | Out-Null

$api = Join-Path $RepoRoot "api\SWCare.Api"
Invoke-Step "API: dotnet publish" {
    dotnet publish $api -c Release -o (Join-Path $Output "api") --nologo
}
# Development settings (local CORS, migrate on start) do not belong on the server.
Remove-Item (Join-Path $Output "api\appsettings.Development.json") -ErrorAction SilentlyContinue

Invoke-Step "Database: idempotent migration script" {
    Push-Location $api
    try {
        dotnet tool restore | Out-Null
        dotnet ef migrations script --idempotent --no-build --configuration Release -o (Join-Path $Output "migrate.sql")
    }
    finally { Pop-Location }
}

Invoke-Step "Sheet importer: dotnet publish" {
    dotnet publish (Join-Path $RepoRoot "tools\SheetImport") -c Release -o (Join-Path $Output "tools\SheetImport") --nologo
}
Copy-Item (Join-Path $RepoRoot "tools\SheetImport\README.md"), (Join-Path $RepoRoot "tools\SheetImport\import-map.example.json") (Join-Path $Output "tools\SheetImport")

$web = Join-Path $RepoRoot "web"
Push-Location $web
try {
    if ($NpmCi -or -not (Test-Path "node_modules")) {
        Invoke-Step "Web: npm ci" { npm ci --no-audit --no-fund }
    }
    Invoke-Step "Web: next build (static export)" { npm run build }
}
finally { Pop-Location }
Copy-Item (Join-Path $web "out") (Join-Path $Output "web") -Recurse

Copy-Item (Join-Path $PSScriptRoot "deploy-iis.ps1"), (Join-Path $PSScriptRoot "backup.ps1") $Output

$commit = (git -C $RepoRoot rev-parse --short HEAD).Trim()
$dirty = if (git -C $RepoRoot status --porcelain) { " (uncommitted changes)" } else { "" }
"SWCare $commit$dirty built $(Get-Date -Format 'yyyy-MM-dd HH:mm')" | Set-Content (Join-Path $Output "VERSION.txt") -Encoding utf8

if ($Zip) {
    $zipPath = Join-Path (Split-Path $Output -Parent) "SWCare-$(Get-Date -Format 'yyyyMMdd-HHmm').zip"
    Compress-Archive -Path (Join-Path $Output "*") -DestinationPath $zipPath -Force
    Write-Host "Zip: $zipPath" -ForegroundColor Green
}

Write-Host "Done: $Output ($commit$dirty)" -ForegroundColor Green
