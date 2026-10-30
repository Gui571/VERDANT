param([string]$Godot = "godot", [switch]$Test)
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
dotnet build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if ($Test) { & $Godot --headless --path . -- --self-test } else { & $Godot --path . }
exit $LASTEXITCODE
