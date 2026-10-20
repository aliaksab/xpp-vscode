$ErrorActionPreference = 'Stop'
$root = 'C:\Users\Mohamed Nazmi\source\repos\xpp-vscode'
$cfg = (Get-ItemProperty 'HKCU:\Software\Microsoft\Dynamics\AX7\Development\Configurations').CurrentMetadataConfig

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = Join-Path $root 'server\bin\XppVsCode.Server.exe'
$psi.Arguments = "--config `"$cfg`""
$psi.UseShellExecute = $false
$psi.RedirectStandardInput = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$sw = [Diagnostics.Stopwatch]::StartNew()
$p = [Diagnostics.Process]::Start($psi)
$out = $p.StandardOutput.ReadToEndAsync()
$err = $p.StandardError.ReadToEndAsync()

$lines = @(
    '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"t","version":"1"}}}',
    '{"jsonrpc":"2.0","method":"notifications/initialized"}',
    '{"jsonrpc":"2.0","id":2,"method":"tools/list"}',
    '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"xpp_list_models","arguments":{}}}',
    '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"xpp_read_object","arguments":{"objectType":"AxClass","objectName":"SalesFormLetter"}}}',
    '{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"xpp_read_object","arguments":{"objectType":"AxTable","objectName":"CustTable"}}}'
)
foreach ($l in $lines) { $p.StandardInput.Write($l + "`n") }
$p.StandardInput.Close()

if (-not $p.WaitForExit(180000)) { $p.Kill(); 'TIMEOUT' }
"exit=$($p.ExitCode) seconds=$([int]$sw.Elapsed.TotalSeconds)"
"--- stderr:"; $err.Result.Substring(0, [Math]::Min(1500, $err.Result.Length))
"--- responses:"
foreach ($r in ($out.Result -split "`n" | Where-Object { $_ })) {
    $o = $r | ConvertFrom-Json
    if ($o.error) { "id=$($o.id) ERROR $($o.error.message)"; continue }
    if ($o.result.tools) { "id=$($o.id) tools: " + (($o.result.tools | ForEach-Object name) -join ', '); continue }
    if ($o.result.serverInfo) { "id=$($o.id) init ok: $($o.result.serverInfo.name) $($o.result.serverInfo.version)"; continue }
    $text = $o.result.content[0].text
    "id=$($o.id) isError=$($o.result.isError) chars=$($text.Length)"
    ($text -split "`n" | Select-Object -First 1)
    if ($o.id -eq 3) { "custom: " + (($text -split "`n" | Where-Object { $_ -match 'custom' }) -join ' | ') }
    if ($o.id -eq 4) { "has 'class SalesFormLetter': $($text -match 'class SalesFormLetter')" }
    if ($o.id -eq 5) { ($text -split "`n" | Select-Object -Skip 1 -First 4) -join ' | ' }
}
