<#
.SYNOPSIS
    Proves that every component the vibe CLI offers compiles after being vendored on its own.

.DESCRIPTION
    For each component in the CLI registry this creates a fresh Blazor app, runs `vibe init`,
    runs `vibe add <component>` for that one component only, and builds the app with warnings
    as errors.

    One app per component is deliberate. A single app with every component added cannot see a
    missing dependency: Select would find Icon simply because `vibe add icon` had already put
    it there. Isolation is the only arrangement in which "vibe add select" has to stand on what
    it installs itself. One extra app with everything added is built as well, because that is
    the only arrangement that can see two components colliding with each other.

    The apps are compiled by a single parallel MSBuild invocation, so the cost is one build
    session rather than one per component.

.PARAMETER ToolExecutable
    Path to the installed vibe tool under test.

.PARAMETER NuGetConfigPath
    NuGet configuration used to restore the generated apps.

.PARAMETER WorkRoot
    Empty or missing directory to generate the apps in. The caller owns its cleanup.

.PARAMETER Components
    Debugging aid: validate only these components. Never passed by CI. A run narrowed this way
    says so loudly and does not build the "everything together" app.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ToolExecutable,
    [Parameter(Mandatory = $true)][string]$NuGetConfigPath,
    [Parameter(Mandatory = $true)][string]$WorkRoot,
    [string[]]$Components = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Components that are known not to compile when vendored and are excluded from the gate.
# Every entry is printed on every run. Keep this empty; an entry here is a debt, with a reason.
$knownUnvendorable = [ordered]@{
}

$allTogetherName = "_AllComponents"

function Invoke-Logged {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList,
        [Parameter(Mandatory = $true)][string]$LogPath
    )

    & $FilePath @ArgumentList *>> $LogPath
    return $LASTEXITCODE
}

function Assert-Step {
    param(
        [Parameter(Mandatory = $true)][int]$ExitCode,
        [Parameter(Mandatory = $true)][string]$What,
        [Parameter(Mandatory = $true)][string]$LogPath
    )

    if ($ExitCode -ne 0) {
        Get-Content -LiteralPath $LogPath | Write-Host
        throw "$What failed with exit code $ExitCode. Log: $LogPath"
    }
}

function Get-RegistryComponentNames {
    $output = & $ToolExecutable list --names
    if ($LASTEXITCODE -ne 0) {
        throw "'vibe list --names' failed with exit code $LASTEXITCODE."
    }

    $names = @($output | ForEach-Object { "$_".Trim() } | Where-Object { $_ -ne "" })
    $malformed = @($names | Where-Object { $_ -notmatch '^[A-Za-z][A-Za-z0-9]*$' })
    if ($malformed.Count -gt 0) {
        throw "'vibe list --names' printed lines that are not component names: $($malformed -join ' | ')"
    }

    return $names
}

if ((Test-Path -LiteralPath $WorkRoot) -and @(Get-ChildItem -LiteralPath $WorkRoot -Force).Count -gt 0) {
    throw "WorkRoot must be empty so a stale build output cannot pass for a fresh one: $WorkRoot"
}

New-Item -ItemType Directory -Force -Path $WorkRoot | Out-Null
$WorkRoot = (Resolve-Path -LiteralPath $WorkRoot).Path

$registryNames = @(Get-RegistryComponentNames)
if ($registryNames.Count -eq 0) {
    throw "The vibe registry reported no components."
}

$isSubset = $Components.Count -gt 0
if ($isSubset) {
    $unknown = @($Components | Where-Object { $registryNames -notcontains $_ })
    if ($unknown.Count -gt 0) {
        throw "Not in the vibe registry (names are case-sensitive here): $($unknown -join ', ')"
    }

    Write-Warning "SUBSET RUN: validating $($Components.Count) of $($registryNames.Count) components. This is not the gate."
    $selectedNames = @($Components)
}
else {
    $selectedNames = $registryNames
}

$staleExclusions = @($knownUnvendorable.Keys | Where-Object { $registryNames -notcontains $_ })
if ($staleExclusions.Count -gt 0) {
    throw "Excluded components are not in the registry; remove them from `$knownUnvendorable: $($staleExclusions -join ', ')"
}

$excludedNames = @($selectedNames | Where-Object { $knownUnvendorable.Contains($_) })
$validatedNames = @($selectedNames | Where-Object { -not $knownUnvendorable.Contains($_) })

foreach ($excludedName in $excludedNames) {
    Write-Warning "EXCLUDED from the vendoring gate: $excludedName. Reason: $($knownUnvendorable[$excludedName])"
}

# One real Blazor app is generated and initialised, then copied per component. The consumer is
# what `dotnet new blazor` and `vibe init` produce, untouched: any using or file a component
# needs beyond that is something a user would be missing too.
$templatePath = Join-Path $WorkRoot "template"
$setupLog = Join-Path $WorkRoot "setup.log"

$exitCode = Invoke-Logged "dotnet" @(
    "new", "blazor",
    "--name", "Consumer",
    "--output", $templatePath,
    "--framework", "net10.0",
    "--interactivity", "Server",
    "--no-restore",
    "--no-update-check"
) $setupLog
Assert-Step $exitCode "dotnet new blazor" $setupLog

$exitCode = Invoke-Logged $ToolExecutable @("init", "--yes", "--path", $templatePath) $setupLog
Assert-Step $exitCode "vibe init" $setupLog

if (-not (Test-Path -LiteralPath (Join-Path $templatePath "Vibe/Base/VibeComponent.cs") -PathType Leaf)) {
    throw "vibe init exited 0 but did not copy the infrastructure into $templatePath."
}

$appsRoot = Join-Path $WorkRoot "apps"
$apps = [System.Collections.Generic.List[object]]::new()
foreach ($componentName in $validatedNames) {
    $apps.Add([pscustomobject]@{ Name = $componentName; Components = @($componentName) })
}

if (-not $isSubset) {
    $apps.Add([pscustomobject]@{ Name = $allTogetherName; Components = @($validatedNames) })
}

$appNames = @($apps | ForEach-Object { $_.Name })

Write-Host "Vendoring $($validatedNames.Count) components, one app each..."
$vendoringFailures = @($apps | ForEach-Object -ThrottleLimit 8 -Parallel {
    $appPath = Join-Path $using:appsRoot $_.Name
    New-Item -ItemType Directory -Force -Path $appPath | Out-Null
    Copy-Item -Path (Join-Path $using:templatePath '*') -Destination $appPath -Recurse -Force

    $logPath = Join-Path $appPath "vibe-add.log"
    foreach ($componentName in $_.Components) {
        & $using:ToolExecutable add $componentName.ToLowerInvariant() --yes --path $appPath *>> $logPath
        if ($LASTEXITCODE -ne 0) {
            return "vibe add $componentName exited $LASTEXITCODE in app '$($_.Name)'. Log: $logPath"
        }

        # Exit 0 only says the tool did not crash; the component file is the receipt.
        $componentFile = Join-Path $appPath "Components/vibe/$componentName.razor"
        if (-not (Test-Path -LiteralPath $componentFile -PathType Leaf)) {
            return "vibe add $componentName exited 0 but did not write $componentFile."
        }
    }
})

if ($vendoringFailures.Count -gt 0) {
    $vendoringFailures | ForEach-Object { Write-Host $_ }
    throw "vibe add failed for $($vendoringFailures.Count) of $($apps.Count) apps."
}

# ContinueOnError keeps one broken component from hiding the rest of the list.
$traversalProject = Join-Path $WorkRoot "apps.proj"
Set-Content -LiteralPath $traversalProject -Encoding utf8NoBOM -Value @"
<Project>
  <ItemGroup>
    <ConsumerApp Include="apps/*/Consumer.csproj" />
  </ItemGroup>
  <Target Name="RestoreApps">
    <MSBuild Projects="@(ConsumerApp)" Targets="Restore" BuildInParallel="true"
             Properties="RestoreConfigFile=$([System.Security.SecurityElement]::Escape($NuGetConfigPath))" />
  </Target>
  <Target Name="BuildApps">
    <MSBuild Projects="@(ConsumerApp)" Targets="Build" BuildInParallel="true" ContinueOnError="true"
             Properties="Configuration=Release;TreatWarningsAsErrors=true" />
  </Target>
</Project>
"@

$restoreLog = Join-Path $WorkRoot "restore.log"
$exitCode = Invoke-Logged "dotnet" @("msbuild", $traversalProject, "-t:RestoreApps", "-m", "-nologo", "-v:minimal") $restoreLog
Assert-Step $exitCode "Restoring the generated apps" $restoreLog

$buildLog = Join-Path $WorkRoot "build.log"
$errorLog = Join-Path $WorkRoot "build-errors.log"
Write-Host "Building $($appNames.Count) apps with warnings as errors..."
$null = Invoke-Logged "dotnet" @(
    "msbuild", $traversalProject,
    "-t:BuildApps", "-m", "-nologo", "-v:minimal",
    "-flp:logfile=$errorLog;errorsonly;encoding=utf-8"
) $buildLog

# The build's exit code is not consulted. Each app is judged by whether its assembly exists:
# the directory was empty before this run, so the assembly is proof that this app compiled.
$errorLines = @()
if (Test-Path -LiteralPath $errorLog -PathType Leaf) {
    $errorLines = @(Get-Content -LiteralPath $errorLog | Where-Object { $_ -match '\S' } | Sort-Object -Unique)
}

$results = foreach ($appName in $appNames) {
    $appPath = Join-Path $appsRoot $appName
    $assembly = Join-Path $appPath "bin/Release/net10.0/Consumer.dll"
    $marker = [System.IO.Path]::DirectorySeparatorChar + $appName + [System.IO.Path]::DirectorySeparatorChar + "Consumer.csproj]"
    $appErrors = @($errorLines | Where-Object { $_.Contains($marker) })

    [pscustomobject]@{
        Name   = $appName
        Passed = (Test-Path -LiteralPath $assembly -PathType Leaf)
        Errors = $appErrors
    }
}
$results = @($results)

# A component that was never built must not read as green.
if ($results.Count -ne $appNames.Count) {
    throw "Internal error: built $($results.Count) results for $($appNames.Count) apps."
}

$builtAppCount = @(Get-ChildItem -LiteralPath $appsRoot -Directory).Count
if ($builtAppCount -ne $appNames.Count) {
    throw "Expected $($appNames.Count) generated apps but found $builtAppCount in $appsRoot."
}

if (-not $isSubset -and ($validatedNames.Count + $excludedNames.Count) -ne $registryNames.Count) {
    throw "Validated ($($validatedNames.Count)) plus excluded ($($excludedNames.Count)) does not equal the registry ($($registryNames.Count))."
}

$inconsistent = @($results | Where-Object { $_.Passed -and $_.Errors.Count -gt 0 })
if ($inconsistent.Count -gt 0) {
    throw "Apps produced an assembly and errors at once, so the gate cannot be trusted: $($inconsistent.Name -join ', ')"
}

$passed = @($results | Where-Object { $_.Passed })
$failed = @($results | Where-Object { -not $_.Passed })

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "Components that do not compile after 'vibe add':"
    foreach ($failure in $failed) {
        $summaries = @(
            $failure.Errors |
                ForEach-Object {
                    if ($_ -match 'error (?<code>[A-Z]+\d+): (?<message>.*?) \[[^\]]*\]\s*$') {
                        "$($Matches.code): $($Matches.message)"
                    }
                    else {
                        $_
                    }
                } |
                Sort-Object -Unique
        )

        Write-Host ""
        Write-Host "  $($failure.Name) ($($summaries.Count) distinct errors)"
        if ($summaries.Count -eq 0) {
            Write-Host "    no assembly and no captured error; see $buildLog"
        }

        foreach ($summary in ($summaries | Select-Object -First 12)) {
            Write-Host "    $summary"
        }

        if ($summaries.Count -gt 12) {
            Write-Host "    ... $($summaries.Count - 12) more in $errorLog"
        }
    }

    Write-Host ""
    Write-Host "Vendoring gate FAILED: $($failed.Count) of $($results.Count) apps did not compile ($($validatedNames.Count) components one at a time$(if (-not $isSubset) { ', plus all of them together' })). Excluded: $($excludedNames.Count). Registry: $($registryNames.Count)."
    Write-Host "Failed: $($failed.Name -join ', ')"
    throw "Vendoring gate failed for $($failed.Count) of $($results.Count) apps."
}

Write-Host "Vendoring gate passed: $($passed.Count) of $($results.Count) apps compiled ($($validatedNames.Count) components one at a time$(if (-not $isSubset) { ', plus all of them together' })). Excluded: $($excludedNames.Count). Registry: $($registryNames.Count)."
