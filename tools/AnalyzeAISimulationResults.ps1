param(
    [string]$SummaryPath = "D:\khans_invasion\khans_invasion_v1\Logs\AIExperiments\ai_experiment_summary.csv",
    [string]$OutputPath = "D:\khans_invasion\khans_invasion_v1\Logs\AIExperiments\ai_tuning_recommendation.md"
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $SummaryPath)) {
    throw "Simulation summary not found: $SummaryPath"
}

$rows = @(Import-Csv -LiteralPath $SummaryPath)
if ($rows.Count -eq 0) {
    throw 'Simulation summary contains no runs.'
}

$passRate = (@($rows | Where-Object { $_.Result -eq 'PASS' }).Count / $rows.Count) * 100.0
$averageTurns = ($rows | Measure-Object -Property TurnsAdvanced -Average).Average
$averageRaids = ($rows | Measure-Object -Property AIRaids -Average).Average
$averageConquests = ($rows | Measure-Object -Property AIConquests -Average).Average
$averageLoot = ($rows | Measure-Object -Property AIRaidLoot -Average).Average

$recommendations = [System.Collections.Generic.List[string]]::new()
if ($passRate -lt 100) {
    $recommendations.Add('Fix failed simulation runs before tuning behavior weights.')
}
if ($averageRaids -lt 2) {
    $recommendations.Add('Raid activity is too low: consider lowering RaidReadinessRatio or MinTroopsBeforeRaid slightly.')
} elseif ($averageRaids -gt 20) {
    $recommendations.Add('Raid activity is too high: consider raising RaidReadinessRatio or lowering AIRaidEffectiveness.')
} else {
    $recommendations.Add('Raid activity is within the initial target band (2-20 raids per 80-turn run).')
}
if ($averageConquests -lt 1) {
    $recommendations.Add('Conquest activity is too low: inspect raid pressure and readiness before reducing conquest gates.')
} elseif ($averageConquests -gt 12) {
    $recommendations.Add('Conquest activity is too high: raise ConquestReadinessRatio or ConquestMinimumProvinceCount.')
} else {
    $recommendations.Add('Conquest activity is within the initial target band (1-12 per 80-turn run).')
}
if ($averageLoot -le 0) {
    $recommendations.Add('AI raids produced no loot: validate target selection and raid eligibility.')
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# AI Simulation Recommendation')
$lines.Add('')
$lines.Add("Runs: $($rows.Count)")
$lines.Add(("Pass rate: {0:F1}%" -f $passRate))
$lines.Add(("Average turns advanced: {0:F1}" -f $averageTurns))
$lines.Add(("Average AI raids: {0:F2}" -f $averageRaids))
$lines.Add(("Average AI conquests: {0:F2}" -f $averageConquests))
$lines.Add(("Average AI raid loot: {0:F2}" -f $averageLoot))
$lines.Add('')
$lines.Add('## Next tuning recommendation')
$lines.Add('')
foreach ($recommendation in $recommendations) {
    $lines.Add("- $recommendation")
}

$directory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Force -Path $directory | Out-Null
[System.IO.File]::WriteAllLines($OutputPath, $lines, [System.Text.UTF8Encoding]::new($false))
