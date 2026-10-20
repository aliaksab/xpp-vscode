param([string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'

$key = 'HKCU:\Software\Microsoft\Dynamics\AX7\Development\Configurations'
$config = (Get-ItemProperty -Path $key -Name CurrentMetadataConfig -ErrorAction SilentlyContinue).CurrentMetadataConfig
if (-not $config -or -not (Test-Path $config)) {
    throw 'No active D365FO configuration. Connect to a unified developer environment first.'
}
$framework = (Get-Content $config -Raw | ConvertFrom-Json).FrameworkDirectory
$bin = Join-Path $framework 'bin'

$root = Split-Path $PSScriptRoot -Parent
dotnet build (Join-Path $root 'server\XppVsCode.Server.csproj') -c $Configuration -o (Join-Path $root 'server\bin') "-p:D365BinPath=$bin" -nologo -v:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
