param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.1.14f1\Editor\Unity.exe",
    [string]$ProjectPath = "D:\khans_invasion\khans_invasion_v1",
    [int]$Turns = 80,
    [int[]]$Seeds = @(101, 202, 303, 404, 505, 606, 707, 808, 909, 1001, 1102, 1203),
    [int]$WaitForEditorMinutes = 360
)

$ErrorActionPreference = 'Stop'
$lockPath = Join-Path $ProjectPath 'Temp\UnityLockfile'
$logsPath = Join-Path $ProjectPath 'Logs\AIExperiments'
New-Item -ItemType Directory -Force -Path $logsPath | Out-Null

$deadline = (Get-Date).AddMinutes($WaitForEditorMinutes)
while (Test-Path -LiteralPath $lockPath) {
    if ((Get-Date) -ge $deadline) {
        throw "Unity project lock remained active for $WaitForEditorMinutes minutes."
    }
    Start-Sleep -Seconds 30
}

$summaryRows = @()
foreach ($seed in $Seeds) {
    $reportName = "simulation_seed_$seed.txt"
    $reportPath = Join-Path (Join-Path $ProjectPath 'Logs') $reportName
    $logPath = Join-Path $logsPath "unity_seed_$seed.log"

    if (Test-Path -LiteralPath $reportPath) { Remove-Item -LiteralPath $reportPath -Force }

    & $UnityPath -batchmode -nographics -quit `
        -projectPath $ProjectPath `
        -executeMethod Khans.Invasion.Testing.SimulationRunner.Run `
        -khansSimTurns $Turns `
        -khansSimInterval 0.1 `
        -khansSimTimeout 180 `
        -khansSimSeed $seed `
        -khansSimReport $reportName `
        -logFile $logPath
    $exitCode = $LASTEXITCODE

    $result = 'MISSING_REPORT'
    $turnsAdvanced = 0
    $aiRaids = 0
    $aiConquests = 0
    $aiRaidLoot = 0.0
    if (Test-Path -LiteralPath $reportPath) {
        $report = Get-Content -LiteralPath $reportPath -Raw
        if ($report -match 'RESULT: (PASS|FAIL)') { $result = $Matches[1] }
        if ($report -match 'Turns advanced: (\d+)') { $turnsAdvanced = [int]$Matches[1] }
        if ($report -match 'AI raids: (\d+)') { $aiRaids = [int]$Matches[1] }
        if ($report -match 'AI conquests: (\d+)') { $aiConquests = [int]$Matches[1] }
        if ($report -match 'AI raid loot: ([\d\.]+)') { $aiRaidLoot = [double]$Matches[1] }
        Copy-Item -LiteralPath $reportPath -Destination (Join-Path $logsPath $reportName) -Force
    }

    [PSCustomObject]@{
        Seed = $seed
        UnityExitCode = $exitCode
        Result = $result
        TurnsAdvanced = $turnsAdvanced
        AIRaids = $aiRaids
        AIConquests = $aiConquests
        AIRaidLoot = $aiRaidLoot
        Report = "Logs/AIExperiments/$reportName"
    } | ForEach-Object { $summaryRows += $_ }
}

$summaryRows | Export-Csv -NoTypeInformation -Path (Join-Path $logsPath 'ai_experiment_summary.csv')
$summaryRows | Format-Table -AutoSize
& (Join-Path $PSScriptRoot 'AnalyzeAISimulationResults.ps1') -SummaryPath (Join-Path $logsPath 'ai_experiment_summary.csv') -OutputPath (Join-Path $logsPath 'ai_tuning_recommendation.md')
