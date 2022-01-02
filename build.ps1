$ErrorActionPreference = 'Stop'

Set-Location -LiteralPath $PSScriptRoot

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

Write-Host "running dotnet tool restore"
dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "error dotnet tool restore"
     exit $LASTEXITCODE
}

Write-Host "running dotnet cake"
dotnet cake @args
if ($LASTEXITCODE -ne 0) {
    Write-Host "error dotnet tool restore"
    exit $LASTEXITCODE
}
