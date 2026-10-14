param([string]$UnityEditor = 'E:/Apps/6000.4.10f1/Editor')
$ErrorActionPreference = 'Stop'
$projectDir = Split-Path $PSScriptRoot -Parent
$outputDir = Join-Path $projectDir 'Build/PolishReview'
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$mono = Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe'
$compiler = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources = @(
    Get-ChildItem (Join-Path $projectDir 'Assets/Script/Core') -Filter '*.cs'
    Get-ChildItem (Join-Path $projectDir 'Assets/Script/Artifacts') -Filter '*.cs'
) | ForEach-Object { $_.FullName }
$sources += Join-Path $projectDir 'Assets/Script/Roguelike/GameManager.cs'
$sources += Join-Path $PSScriptRoot 'CoreRegression.cs'
$program = Join-Path $outputDir 'CoreRegression.exe'
& $mono $compiler /nologo /langversion:9 "/out:$program" $sources
if ($LASTEXITCODE -ne 0) { throw 'Core regression compilation failed.' }
& $mono $program
if ($LASTEXITCODE -ne 0) { throw 'Core regression checks failed.' }
