$ErrorActionPreference = 'Stop'
$culture = [Globalization.CultureInfo]::InvariantCulture
function Number([string] $value) { [double]::Parse($value, $culture) }
$results = foreach ($file in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.csv') {
    if ($file.BaseName -notmatch '^(baseline-forward|acceptance-.+)$' -or $file.BaseName -match '-camera$') { continue }
    $rows = @(Import-Csv -LiteralPath $file.FullName)
    $recovery = @($rows | Where-Object state -eq 'Recovering')
    $changes = @()
    $lastState = ''
    foreach ($row in $rows) {
        if ($row.state -ne $lastState) {
            $changes += ('{0:F2}s {1}' -f (Number $row.time), $row.state)
            $lastState = $row.state
        }
    }
    [pscustomobject]@{
        Case = $file.BaseName
        FinalState = $rows[-1].state
        Transitions = $changes
        RecoverySeconds = $recovery.Count * 0.02
        PeakRecoveryDegreesPerStep = ($recovery | ForEach-Object { Number $_.rotationStep } | Measure-Object -Maximum).Maximum
        PeakRecoveryAcceleration = ($recovery | ForEach-Object { Number $_.acceleration } | Measure-Object -Maximum).Maximum
        PeakRecoveryAnchorSeparation = ($recovery | ForEach-Object { Number $_.maxAnchorError } | Measure-Object -Maximum).Maximum
        FinalTilt = Number $rows[-1].tilt
        FinalSpeed = Number $rows[-1].speed
    }
}
$json = $results | ConvertTo-Json -Depth 4
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'acceptance-summary.json'), $json)
$json
