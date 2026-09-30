param(
    [Parameter(Mandatory = $true)][string]$UnityEditor,
    [switch]$UseIsolatedCopy
)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$results = Join-Path $project 'TestResults/WaferSaw'
New-Item -ItemType Directory -Path $results -Force | Out-Null
$testProject = $project
if ($UseIsolatedCopy) {
    $testProject = Join-Path $project 'TestResults/WaferSawValidation'
    $openEditor = Get-CimInstance Win32_Process -Filter "name = 'Unity.exe'" | Where-Object { $_.CommandLine -like "*$testProject*" }
    if ($openEditor) { throw 'The isolated validation project is in use. Close that Editor before copying.' }
    New-Item -ItemType Directory -Path $testProject -Force | Out-Null
    foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
        Copy-Item -LiteralPath (Join-Path $project $folder) -Destination $testProject -Recurse -Force
    }
}
function Invoke-WaferSawUnity([string[]]$Extra, [string]$LogName) {
    $arguments = @('-batchmode', '-force-d3d11', '-projectPath', "`"$testProject`"", '-logFile', "`"$results/$LogName`"") + $Extra
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) { throw "Unity exit $($process.ExitCode). See $results/$LogName" }
}
Invoke-WaferSawUnity -Extra @('-executeMethod', 'WaferSaw.Editor.WaferSawSceneBuilder.BuildBatch') -LogName 'build.log'
foreach ($mode in @('EditMode', 'PlayMode')) {
    Invoke-WaferSawUnity -Extra @('-runTests', '-testPlatform', $mode, '-assemblyNames', "WaferSaw.$($mode)Tests", '-testResults', "`"$results/$mode.xml`"") -LogName "$mode.log"
    [xml]$report = Get-Content -LiteralPath "$results/$mode.xml"
    $run = $report.'test-run'
    if ($run.result -ne 'Passed' -or [int]$run.total -eq 0) { throw "$mode tests did not pass." }
    Write-Output "$mode : $($run.passed)/$($run.total) passed"
}
