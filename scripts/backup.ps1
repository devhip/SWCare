# Nightly backup (plan 11): the database and the attachments folder, kept 30 days, optionally copied to a NAS.
# Register once on the server (docs/Deploy.md):
#
#   .\backup.ps1                                   # run now
#   .\backup.ps1 -CopyTo \\nas\backup\SWCare        # also copy to another machine
#
# The SQL Server service account must be able to write to $BackupRoot.
param(
    [string]$SqlServer = ".\SQLEXPRESS",
    [string]$Database = "SWCare",
    [string]$BackupRoot = "C:\SWCareData\backup",
    [string]$Attachments = "C:\SWCareData\attachments",
    [int]$KeepDays = 30,
    [string]$CopyTo = ""
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force $BackupRoot | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmm"
$bak = Join-Path $BackupRoot "$Database-$stamp.bak"

sqlcmd -S $SqlServer -E -C -b -Q "BACKUP DATABASE [$Database] TO DISK = N'$bak' WITH INIT, CHECKSUM" | Out-Host
if ($LASTEXITCODE -ne 0) { throw "Database backup failed ($LASTEXITCODE)" }

# Attachments are never deleted in SWCare (Q54), so a mirror keeps every file.
robocopy $Attachments (Join-Path $BackupRoot "attachments") /MIR /R:3 /W:5 /NFL /NDL /NJH /NP | Out-Host
if ($LASTEXITCODE -ge 8) { throw "Attachments copy failed ($LASTEXITCODE)" }
$global:LASTEXITCODE = 0

Get-ChildItem $BackupRoot -Filter "$Database-*.bak" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$KeepDays) } |
    Remove-Item

if ($CopyTo) {
    robocopy $BackupRoot $CopyTo /MIR /R:3 /W:5 /NFL /NDL /NJH /NP | Out-Host
    if ($LASTEXITCODE -ge 8) { throw "Copy to $CopyTo failed ($LASTEXITCODE)" }
    $global:LASTEXITCODE = 0
}

Write-Host "Backup done: $bak"
