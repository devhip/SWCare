#Requires -RunAsAdministrator
# Installs or updates SWCare on the IIS server (docs/Deploy.md). Run ON the server, from the folder made by publish.ps1:
#
#   .\deploy-iis.ps1                                  # first time: creates folders, app pools, sites, database, settings
#   .\deploy-iis.ps1                                  # later: same command updates api + web and migrates the database
#   .\deploy-iis.ps1 -ConnectionString "Server=sql01;Database=SWCare;User Id=swcare;Password=...;TrustServerCertificate=True"
#
# Kept on the server (never overwritten): api\web.config (holds the settings), api\appsettings.Production.json,
# api\logs, web\config.json, web\web.config, the attachments folder.
param(
    [string]$InstallRoot = "C:\SWCare",
    [string]$DataRoot = "C:\SWCareData",
    [int]$WebPort = 80,
    [int]$ApiPort = 5300,
    # Address people type in the browser, e.g. http://192.168.1.20 - default: this server's first IPv4
    [string]$PublicHost = "",
    [string]$SqlServer = ".\SQLEXPRESS",
    [string]$Database = "SWCare",
    # Empty = Windows login of the API app pool on $SqlServer (created by this script)
    [string]$ConnectionString = "",
    [switch]$SkipMigration
)

$ErrorActionPreference = "Stop"
$Package = $PSScriptRoot
$ApiPath = Join-Path $InstallRoot "api"
$WebPath = Join-Path $InstallRoot "web"
$Attachments = Join-Path $DataRoot "attachments"
$ApiPool = "SWCare.Api"
$WebPool = "SWCare.Web"
$appcmd = Join-Path $env:windir "System32\inetsrv\appcmd.exe"

function Assert-Ready {
    if (-not (Test-Path $appcmd)) { throw "IIS is not installed (appcmd.exe not found)." }
    $module = Join-Path $env:ProgramFiles "IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
    if (-not (Test-Path $module)) { throw "ASP.NET Core Hosting Bundle 10 is not installed (https://dotnet.microsoft.com/download/dotnet/10.0)." }
    foreach ($part in "api", "web") {
        if (-not (Test-Path (Join-Path $Package $part))) { throw "Run this script from the folder made by publish.ps1 (missing $part\)." }
    }
}

function Invoke-AppCmd([string[]]$Arguments) {
    $text = & $appcmd @Arguments 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "appcmd $($Arguments -join ' ') failed: $text" }
    $global:LASTEXITCODE = 0
}

function Test-AppCmd([string]$Kind, [string]$Name) {
    $found = & $appcmd list $Kind $Name 2>$null
    $global:LASTEXITCODE = 0
    return [bool]$found
}

function Ensure-Site([string]$Site, [string]$Pool, [string]$Path, [int]$Port) {
    if (-not (Test-AppCmd "apppool" $Pool)) {
        Invoke-AppCmd @("add", "apppool", "/name:$Pool", "/managedRuntimeVersion:")
        Write-Host "  app pool $Pool created"
    }
    if (-not (Test-AppCmd "site" $Site)) {
        Invoke-AppCmd @("add", "site", "/name:$Site", "/physicalPath:$Path", "/bindings:http/*:${Port}:")
        Invoke-AppCmd @("set", "app", "$Site/", "/applicationPool:$Pool")
        Write-Host "  site $Site created on port $Port"
    }
}

function Get-PoolState([string]$Pool) { (& $appcmd list apppool $Pool /text:state | Out-String).Trim() }

function Stop-Pool([string]$Pool) {
    if ((Get-PoolState $Pool) -eq "Stopped") { return }
    Invoke-AppCmd @("stop", "apppool", "/apppool.name:$Pool")
    for ($i = 0; $i -lt 30 -and (Get-PoolState $Pool) -ne "Stopped"; $i++) { Start-Sleep -Seconds 1 }
    Start-Sleep -Seconds 2 # w3wp releases the dlls
}

function Start-Pool([string]$Pool) { Invoke-AppCmd @("start", "apppool", "/apppool.name:$Pool") }

function Copy-Folder([string]$Source, [string]$Target, [string[]]$KeepFiles, [string[]]$KeepDirs) {
    $arguments = @($Source, $Target, "/E", "/R:5", "/W:2", "/NFL", "/NDL", "/NJH", "/NP")
    if ($KeepFiles) { $arguments += "/XF"; $arguments += $KeepFiles }
    if ($KeepDirs) { $arguments += "/XD"; $arguments += $KeepDirs }
    robocopy @arguments | Out-Host
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed ($LASTEXITCODE): $Source -> $Target" }
    $global:LASTEXITCODE = 0
}

function Invoke-Sql([string]$Query, [string]$File = "") {
    $arguments = @("-S", $SqlServer, "-E", "-C", "-b")
    if ($File) { $arguments += @("-d", $Database, "-i", $File) } else { $arguments += @("-Q", $Query) }
    sqlcmd @arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed ($LASTEXITCODE)" }
}

function New-Secret([int]$Length) {
    $chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789".ToCharArray()
    $bytes = New-Object byte[] $Length
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    -join ($bytes | ForEach-Object { $chars[$_ % $chars.Length] })
}

function Get-UserCount {
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) { return -1 }
    $text = sqlcmd -S $SqlServer -E -C -b -h -1 -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM Users" 2>$null | Out-String
    $global:LASTEXITCODE = 0
    $count = 0
    if ([int]::TryParse($text.Trim(), [ref]$count)) { return $count }
    return -1
}

# Settings live as environment variables in api\web.config; only missing ones are added, except $Replace.
function Set-ApiVariables([string]$ConfigPath, [hashtable]$Values, [string[]]$Replace = @()) {
    [xml]$config = Get-Content $ConfigPath -Raw -Encoding UTF8
    $core = $config.SelectSingleNode("//aspNetCore")
    $variables = $core.SelectSingleNode("environmentVariables")
    if (-not $variables) { $variables = $core.AppendChild($config.CreateElement("environmentVariables")) }
    $added = @()
    foreach ($name in $Values.Keys) {
        $existing = $variables.SelectSingleNode("environmentVariable[@name='$name']")
        if ($existing -and $Replace -contains $name) { [void]$variables.RemoveChild($existing) }
        elseif ($existing) { continue }
        $node = $config.CreateElement("environmentVariable")
        $node.SetAttribute("name", $name)
        $node.SetAttribute("value", $Values[$name])
        [void]$variables.AppendChild($node)
        $added += $name
    }
    $config.Save($ConfigPath)
    return $added
}

Assert-Ready
if (-not $PublicHost) {
    $ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254.*" } | Select-Object -First 1).IPAddress
    $PublicHost = "http://$ip"
}
$PublicHost = $PublicHost.TrimEnd("/")
$WebOrigin = if ($WebPort -eq 80) { $PublicHost } else { "${PublicHost}:$WebPort" }
$ApiUrl = "${PublicHost}:$ApiPort"
$firstInstall = -not (Test-Path (Join-Path $ApiPath "SWCare.Api.dll"))

Write-Host "==> SWCare $(Get-Content (Join-Path $Package 'VERSION.txt') -ErrorAction SilentlyContinue)" -ForegroundColor Cyan
Write-Host "    web $WebOrigin | api $ApiUrl | $(if ($firstInstall) { 'first install' } else { 'update' })"

# 1. Folders, app pools, sites
foreach ($path in $ApiPath, $WebPath, $Attachments, (Join-Path $ApiPath "logs")) { New-Item -ItemType Directory -Force $path | Out-Null }
Ensure-Site "SWCare.Api" $ApiPool $ApiPath $ApiPort
Ensure-Site "SWCare.Web" $WebPool $WebPath $WebPort
foreach ($path in $Attachments, (Join-Path $ApiPath "logs")) {
    icacls $path /grant "IIS AppPool\${ApiPool}:(OI)(CI)M" /Q | Out-Null
}
if (-not (Get-NetFirewallRule -DisplayName "SWCare" -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName "SWCare" -Direction Inbound -Protocol TCP -LocalPort $WebPort, $ApiPort -Action Allow -Profile Domain, Private | Out-Null
    Write-Host "  firewall: ports $WebPort, $ApiPort open (domain / private networks)"
}

# 2. Database
if (-not $SkipMigration) {
    Write-Host "==> Database" -ForegroundColor Cyan
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        throw "sqlcmd not found. Install it, or run migrate.sql in SSMS and use -SkipMigration."
    }
    Invoke-Sql "IF DB_ID(N'$Database') IS NULL CREATE DATABASE [$Database];"
    if (-not $ConnectionString) {
        $login = "IIS APPPOOL\$ApiPool"
        Invoke-Sql @"
IF SUSER_ID(N'$login') IS NULL CREATE LOGIN [$login] FROM WINDOWS;
USE [$Database];
IF USER_ID(N'$login') IS NULL CREATE USER [$login] FOR LOGIN [$login];
ALTER ROLE db_datareader ADD MEMBER [$login];
ALTER ROLE db_datawriter ADD MEMBER [$login];
"@
    }
    Invoke-Sql "" (Join-Path $Package "migrate.sql")
}

# 3. API
Write-Host "==> API" -ForegroundColor Cyan
Stop-Pool $ApiPool
$apiConfig = Join-Path $ApiPath "web.config"
$keepApi = @("appsettings.Production.json")
if (Test-Path $apiConfig) { $keepApi += "web.config" }
Copy-Folder (Join-Path $Package "api") $ApiPath $keepApi @("logs")

# No account yet = the API creates "admin" at start with this password (DbSeeder). Its own fallback prints to a
# console that IIS does not show, so the script always supplies one.
$adminPassword = $null
$users = Get-UserCount
if ($users -eq 0 -or ($users -lt 0 -and $firstInstall)) { $adminPassword = New-Secret 16 }
$connection = if ($ConnectionString) { $ConnectionString } else { "Server=$SqlServer;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True" }
$settings = @{
    "ConnectionStrings__SWCare"   = $connection
    "Jwt__Key"                    = (New-Secret 64)
    "Cors__AllowedOrigins__0"     = $WebOrigin
    "Storage__AttachmentsPath"    = $Attachments
}
if ($adminPassword) { $settings["Seed__AdminPassword"] = $adminPassword }
$added = Set-ApiVariables $apiConfig $settings @("Seed__AdminPassword")
if ($added) { Write-Host "  web.config: added $($added -join ', ')" }
Start-Pool $ApiPool

# 4. Web
Write-Host "==> Web" -ForegroundColor Cyan
$webConfigJson = Join-Path $WebPath "config.json"
$keepWeb = @()
if (Test-Path $webConfigJson) { $keepWeb += "config.json" }
if (Test-Path (Join-Path $WebPath "web.config")) { $keepWeb += "web.config" }
Stop-Pool $WebPool
Copy-Folder (Join-Path $Package "web") $WebPath $keepWeb @()
if ($keepWeb -notcontains "config.json") {
    "{ `"apiUrl`": `"$ApiUrl`" }" | Set-Content $webConfigJson -Encoding utf8
    Write-Host "  config.json: apiUrl = $ApiUrl"
}
Start-Pool $WebPool

# 5. Check
Write-Host "==> Check" -ForegroundColor Cyan
$ok = $false
for ($i = 0; $i -lt 20 -and -not $ok; $i++) {
    try { $ok = (Invoke-WebRequest "http://localhost:$ApiPort/health" -UseBasicParsing -TimeoutSec 10).StatusCode -eq 200 }
    catch { Start-Sleep -Seconds 2 }
}
if (-not $ok) { throw "API did not answer http://localhost:$ApiPort/health - see $ApiPath\logs and the Windows Event Log (IIS AspNetCore Module)." }
Write-Host "  API ok" -ForegroundColor Green
Write-Host ""
Write-Host "SWCare is at $WebOrigin" -ForegroundColor Green
if ($adminPassword) {
    Write-Host ""
    Write-Host "First login: user admin, one-time password $adminPassword" -ForegroundColor Yellow
    Write-Host "It must be changed at the first login. Then remove Seed__AdminPassword from $apiConfig." -ForegroundColor Yellow
}
