param(
    [string] $FrontendUrl = $env:FRONTEND_URL,
    [string] $ApiUrl = $env:API_URL
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($FrontendUrl)) {
    throw 'Set FRONTEND_URL or pass -FrontendUrl. Example: https://<ROOT_DOMAIN>'
}

if ([string]::IsNullOrWhiteSpace($ApiUrl)) {
    $ApiUrl = $FrontendUrl
}

$FrontendUrl = $FrontendUrl.TrimEnd('/')
$ApiUrl = $ApiUrl.TrimEnd('/')

function Test-SmokeEndpoint {
    param(
        [string] $Name,
        [string] $Url,
        [string] $ExpectedSnippet
    )

    $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 30
    if ([int]$response.StatusCode -ne 200) {
        throw "FAIL ${Name}: expected HTTP 200 from ${Url}, got $($response.StatusCode)"
    }

    if ($ExpectedSnippet -and ($response.Content -notlike "*${ExpectedSnippet}*")) {
        throw "FAIL ${Name}: response from ${Url} did not contain expected content"
    }

    Write-Host "OK   ${Name}"
}

Test-SmokeEndpoint -Name 'frontend' -Url "${FrontendUrl}/" -ExpectedSnippet 'app-root'
Test-SmokeEndpoint -Name 'api live' -Url "${ApiUrl}/health/live" -ExpectedSnippet '"status":"Healthy"'
Test-SmokeEndpoint -Name 'api ready' -Url "${ApiUrl}/health" -ExpectedSnippet '"status":"Healthy"'

Write-Host 'Smoke tests passed.'
