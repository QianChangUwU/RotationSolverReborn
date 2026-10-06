param(
    [string]$DalamudHome = $env:DALAMUD_HOME,
    [string]$CompilerProps = $env:RSR_COMPILER_PROPS,
    [string]$OutputDirectory = 'release'
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path "$DalamudHome/Dalamud.dll")) { throw 'Set DALAMUD_HOME to a valid Dalamud installation' }
if (!(Test-Path $CompilerProps)) { throw 'Set RSR_COMPILER_PROPS to Roslyn 5.9 compiler props' }
[xml]$versionFile = Get-Content "$PSScriptRoot/../Directory.Build.targets"
$version = [string]$versionFile.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw "Invalid release version: $version" }

dotnet build "$PSScriptRoot/../RotationSolver.sln" -c Release "-p:DALAMUD_HOME=$DalamudHome" "-p:CustomBeforeMicrosoftCommonTargets=$CompilerProps"
if ($LASTEXITCODE -ne 0) { throw 'Release build failed' }
dotnet run --project "$PSScriptRoot/../tests/LocalizationSmoke/LocalizationSmoke.csproj" -c Release
if ($LASTEXITCODE -ne 0) { throw 'Localization checks failed' }

New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$zipPath = "$PSScriptRoot/../bin/Release/RotationSolver/latest.zip"
$zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entry = $zip.GetEntry('RotationSolver.json')
    if (!$entry) { throw 'Packaged manifest is missing' }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { $manifestText = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $manifest = $manifestText | ConvertFrom-Json
    if ($manifest.InternalName -ne 'RotationSolver' -or $manifest.AssemblyVersion -ne $version) {
        throw 'Packaged identity/version does not match the release'
    }
    foreach ($sourcePath in @('manifest.json', 'RotationSolver/RotationSolver.json')) {
        $source = Get-Content "$PSScriptRoot/../$sourcePath" -Raw | ConvertFrom-Json
        foreach ($field in @('Name', 'Description', 'Punchline')) {
            if ($source.$field -ne $manifest.$field) { throw "Packaged $field differs from $sourcePath" }
        }
    }
    [IO.File]::WriteAllText((Join-Path (Resolve-Path $OutputDirectory) 'RotationSolver.json'), $manifestText)
} finally { $zip.Dispose() }
Copy-Item $zipPath "$OutputDirectory/latest.zip" -Force
foreach ($name in @('latest.zip', 'RotationSolver.json')) {
    $hash = (Get-FileHash "$OutputDirectory/$name" -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $name" | Set-Content "$OutputDirectory/$name.sha256" -Encoding utf8NoBOM
}
