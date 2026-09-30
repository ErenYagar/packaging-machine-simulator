param(
    [Parameter(Mandatory = $true)][string]$UnityEditor,
    [switch]$UseIsolatedCopy
)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$results = Join-Path $project 'TestResults'
New-Item -ItemType Directory -Path $results -Force | Out-Null
$testProject = $project
if ($UseIsolatedCopy) {
    $testProject = Join-Path $results 'ValidationProject'
    New-Item -ItemType Directory -Path $testProject -Force | Out-Null
    foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
        Copy-Item -LiteralPath (Join-Path $project $folder) -Destination $testProject -Recurse -Force
    }
}
function Invoke-UnityCheck([string[]]$Extra, [string]$LogName) {
    $arguments = @('-batchmode', '-projectPath', "`"$testProject`"", '-logFile', "`"$results/$LogName`"") + $Extra
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity exit $($process.ExitCode). See $results/$LogName" }
}
Invoke-UnityCheck -Extra @('-executeMethod', 'PackagingSim.Editor.DemoSceneBuilder.PrepareBatch') -LogName 'prepare.log'
foreach ($mode in @('EditMode', 'PlayMode')) {
    Invoke-UnityCheck -Extra @('-runTests', '-testPlatform', $mode, '-testResults', "`"$results/$mode.xml`"") -LogName "$mode.log"
    [xml]$report = Get-Content -LiteralPath "$results/$mode.xml"
    $run = $report.'test-run'
    if ($run.result -ne 'Passed' -or [int]$run.total -eq 0) { throw "$mode tests did not pass." }
    Write-Output "$mode : $($run.passed)/$($run.total) passed"
}
