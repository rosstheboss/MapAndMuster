param(
    [string] $ConnectionString = $env:ConnectionStrings__Campaign
)

$ErrorActionPreference = 'Stop'

function Get-QuotedNpgsqlValue([string] $value)
{
    return "'" + ($value -replace "'", "''") + "'"
}

function ConvertTo-NpgsqlConnectionString([string] $value)
{
    $value = $value.Trim().Trim([char]0xFEFF).Trim('`').Trim('"').Trim("'").TrimStart('<').TrimEnd('>')
    if ([string]::IsNullOrWhiteSpace($value))
    {
        throw 'The connection string is empty after trimming. Pass the Render External Database URL in single quotes.'
    }

    if ($value -notmatch '^(postgres|postgresql)://')
    {
        return $value
    }

    try
    {
        $uri = [Uri]$value
    }
    catch
    {
        throw 'Could not parse the Render URL. Copy only the External Database URL, and wrap it in single quotes.'
    }

    if (-not $uri.IsAbsoluteUri -or [string]::IsNullOrWhiteSpace($uri.Host))
    {
        throw 'Could not parse the Render URL. Copy only the External Database URL, and wrap it in single quotes.'
    }

    $userInfo = $uri.GetComponents([System.UriComponents]::UserInfo, [System.UriFormat]::Unescaped)
    $separator = $userInfo.IndexOf(':')
    $user = if ($separator -lt 0) { $userInfo } else { $userInfo.Substring(0, $separator) }
    $password = if ($separator -lt 0) { '' } else { $userInfo.Substring($separator + 1) }
    $database = [Uri]::UnescapeDataString($uri.AbsolutePath.Trim('/')).Trim('`')
    $port = if ($uri.IsDefaultPort -or $uri.Port -le 0) { 5432 } else { $uri.Port }

    return @(
        "Host=$($uri.Host)"
        "Port=$port"
        "Database=$(Get-QuotedNpgsqlValue $database)"
        "Username=$(Get-QuotedNpgsqlValue $user)"
        "Password=$(Get-QuotedNpgsqlValue $password)"
        'SSL Mode=Require'
        'Trust Server Certificate=true'
    ) -join ';'
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw 'Set ConnectionStrings__Campaign or pass -ConnectionString. Refusing to migrate an unspecified database.'
}

$root = Split-Path -Parent $PSScriptRoot
$bundle = Join-Path $root 'artifacts/efbundle.exe'
if (-not (Test-Path $bundle)) {
    throw 'No migration bundle found. Run eng/build-migrations.ps1 first. On Windows the file must be artifacts/efbundle.exe.'
}

$normalized = ConvertTo-NpgsqlConnectionString $ConnectionString
& $bundle '--connection' $normalized
if ($LASTEXITCODE -ne 0) {
    throw "Migration bundle failed with exit code $LASTEXITCODE."
}
