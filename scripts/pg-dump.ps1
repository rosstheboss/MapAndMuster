param(
    [string] $ConnectionString = $env:ConnectionStrings__Campaign,
    [string] $OutputPath = $env:BACKUP_PATH
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw 'Set ConnectionStrings__Campaign or pass -ConnectionString. Refusing to dump an unspecified database.'
}

if ($ConnectionString -notmatch '^postgres(ql)?://') {
    throw 'Pass a postgres:// URI from the Render dashboard, not an ASP.NET Host= connection string.'
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = "campaign-$([DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')).dump"
}

$outputDirectory = Split-Path -Parent $OutputPath
if ([string]::IsNullOrWhiteSpace($outputDirectory)) {
    $outputDirectory = (Get-Location).Path
}
else {
    $outputDirectory = (Resolve-Path $outputDirectory).Path
}

$outputFile = Split-Path -Leaf $OutputPath
Write-Host "Writing a custom-format dump to $outputDirectory\$outputFile"

docker run --rm `
    -e "PGDATABASE_URI=$ConnectionString" `
    -e "OUTPUT_FILE=$outputFile" `
    -v "${outputDirectory}:/backup" `
    postgres:17 `
    sh -c 'pg_dump --dbname="$PGDATABASE_URI" --format=custom --no-owner --file="/backup/$OUTPUT_FILE"'

if ($LASTEXITCODE -ne 0) {
    throw "pg_dump failed with exit code $LASTEXITCODE."
}

Write-Host 'Dump finished. Store this file outside the primary database provider.'
