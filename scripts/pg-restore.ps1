param(
    [string] $ConnectionString = $env:ConnectionStrings__Campaign,
    [string] $BackupPath = $env:BACKUP_PATH,
    [string] $Confirm = $env:CONFIRM_RESTORE
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ConnectionString) -or [string]::IsNullOrWhiteSpace($BackupPath)) {
    throw 'Pass -ConnectionString <postgres:// URI> and -BackupPath <file.dump>. Restore only onto a non-production database.'
}

if ($Confirm -ne 'I_UNDERSTAND_THIS_OVERWRITES_THE_TARGET_DATABASE') {
    throw 'Set CONFIRM_RESTORE or pass -Confirm I_UNDERSTAND_THIS_OVERWRITES_THE_TARGET_DATABASE. Never point this at production.'
}

if ($ConnectionString -notmatch '^postgres(ql)?://') {
    throw 'Pass a postgres:// URI from the Render dashboard, not an ASP.NET Host= connection string.'
}

if (-not (Test-Path $BackupPath)) {
    throw 'Backup file not found.'
}

$backupItem = Get-Item $BackupPath
Write-Host "Restoring $($backupItem.FullName) onto the target database"

docker run --rm `
    -e "PGDATABASE_URI=$ConnectionString" `
    -e "BACKUP_FILE=$($backupItem.Name)" `
    -v "$($backupItem.DirectoryName):/backup" `
    postgres:17 `
    sh -c 'pg_restore --clean --if-exists --no-owner --no-acl --dbname="$PGDATABASE_URI" "/backup/$BACKUP_FILE"'

if ($LASTEXITCODE -ne 0) {
    throw "pg_restore failed with exit code $LASTEXITCODE."
}

Write-Host 'Restore finished. Validate the target application before considering the backup good.'
