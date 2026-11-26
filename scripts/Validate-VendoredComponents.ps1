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
    it installs itself.

    Two more apps get every component at once, because that is the only arrangement that can
    see two components colliding with each other: one Server app, and one hosted app where
    `vibe init` installs into the WebAssembly client, which compiles against a smaller
    framework than the server does.

    Two more apps check the FluentValidation adapter, which `vibe init --with-fluentvalidation`
    copies as source: a Server app and a hosted app's WebAssembly client, each with a model, a
    validator, `AddVibeFluentValidation()` in Program.cs and a form using `<FluentValidator />`.
    They add no components, so the adapter has to stand on `vibe init` alone. The fixture uses
    every adapter file, so a file missing from the package fails the build.

    Two more apps check `vibe update --infrastructure`. One is initialised, given components,
    then damaged the way an older or edited project would be (shared types and metadata
    missing, a base class replaced, no version stamp) and refreshed. The other is a hosted app
    initialised without the adapter, which `vibe update --infrastructure --with-fluentvalidation`
    then adds, run from the solution root as a user would. Each must then compile, its Vibe/ tree
    must equal a freshly initialised one file for file, and what update promises not to touch
    (components, the components directory, the colour in vibe-base.css) must be unchanged.

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
    says so loudly and does not build the "everything together" apps. The FluentValidation apps
    are built either way.
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

# Short names: the WebAssembly build nests deep paths, and Windows stops at 260 characters.
$allTogetherName = "_All"
$allTogetherWasmName = "_AllWasm"
$fluentValidationName = "_FV"
$fluentValidationWasmName = "_FVWasm"
$updateName = "_Upd"
$updateWasmName = "_UpdWasm"

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

function New-ConsumerTemplate {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$What,
        [Parameter(Mandatory = $true)][string[]]$NewArguments,
        [string[]]$InitArguments = @()
    )

    $exitCode = Invoke-Logged "dotnet" (@(
        "new", "blazor",
        "--name", "Consumer",
        "--output", $Path,
        "--framework", "net10.0",
        "--no-restore",
        "--no-update-check"
    ) + $NewArguments) $setupLog
    Assert-Step $exitCode "dotnet new blazor ($What)" $setupLog

    $exitCode = Invoke-Logged $ToolExecutable (@("init", "--yes", "--path", $Path) + $InitArguments) $setupLog
    Assert-Step $exitCode "vibe init ($What)" $setupLog
}

# The code an app writes to use the adapter: a model and its validator, the registrations in
# Program.cs, and a form with <FluentValidator />. The page relies on the Razor using that
# `vibe init --with-fluentvalidation` adds, so a missing using fails here as RZ10012.
function Add-FluentValidationFixture {
    param(
        [Parameter(Mandatory = $true)][string]$ComponentsProject,
        [Parameter(Mandatory = $true)][string]$PagesDirectory,
        [Parameter(Mandatory = $true)][string[]]$ProgramFiles
    )

    $adapterDirectory = Join-Path $ComponentsProject "Vibe/FluentValidation"
    if (-not (Test-Path -LiteralPath $adapterDirectory -PathType Container)) {
        throw "The CLI exited 0 but did not copy the FluentValidation adapter into $ComponentsProject."
    }

    $validationDirectory = Join-Path $ComponentsProject "Validation"
    New-Item -ItemType Directory -Force -Path $validationDirectory | Out-Null
    Set-Content -LiteralPath (Join-Path $validationDirectory "Person.cs") -Encoding utf8NoBOM -Value @'
using FluentValidation;

namespace Consumer.Validation;

public sealed class Person
{
    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }
}

public sealed class PersonValidator : AbstractValidator<Person>
{
    public PersonValidator()
    {
        RuleFor(person => person.Name).NotEmpty().MaximumLength(50);
        RuleFor(person => person.Age).InclusiveBetween(0, 150);
    }
}
'@

    # Where the template keeps its own pages, so the page sees the template's _Imports.razor.
    if (-not (Test-Path -LiteralPath $PagesDirectory -PathType Container)) {
        throw "The template has no pages directory at $PagesDirectory."
    }
    Set-Content -LiteralPath (Join-Path $PagesDirectory "PersonForm.razor") -Encoding utf8NoBOM -Value @'
@page "/person"
@using Consumer.Validation

<EditForm Model="person" OnValidSubmit="Save">
    <FluentValidator />
    <InputText @bind-Value="person.Name" />
    <ValidationSummary />
    <button type="submit">Save</button>
</EditForm>

<p>@saved</p>

@code {
    private readonly Person person = new();
    private int saved;

    private void Save() => saved++;
}
'@

    $registrations = @(
        "Vibe.UI.FluentValidationServiceCollectionExtensions.AddVibeFluentValidation(builder.Services);",
        "Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<global::FluentValidation.IValidator<Consumer.Validation.Person>, Consumer.Validation.PersonValidator>(builder.Services);"
    )
    foreach ($programFile in $ProgramFiles) {
        $lines = [System.Collections.Generic.List[string]]::new([string[]](Get-Content -LiteralPath $programFile))
        $builderLine = -1
        for ($index = 0; $index -lt $lines.Count; $index++) {
            if ($lines[$index] -match '^\s*var builder\s*=') {
                $builderLine = $index
                break
            }
        }

        if ($builderLine -lt 0) {
            throw "No 'var builder =' line to register the validator after in $programFile."
        }

        $lines.InsertRange($builderLine + 1, [string[]]$registrations)
        Set-Content -LiteralPath $programFile -Encoding utf8NoBOM -Value $lines
    }
}

function Get-TreeHashes {
    param([Parameter(Mandatory = $true)][string]$Path)

    $hashes = [ordered]@{}
    foreach ($file in @(Get-ChildItem -LiteralPath $Path -File -Recurse | Sort-Object FullName)) {
        $relative = [System.IO.Path]::GetRelativePath($Path, $file.FullName).Replace('\', '/')
        $hashes[$relative] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    }

    return $hashes
}

# The receipt for a refresh: the same files with the same content as a fresh `vibe init`.
function Assert-SameTree {
    param(
        [Parameter(Mandatory = $true)][string]$Expected,
        [Parameter(Mandatory = $true)][string]$Actual,
        [Parameter(Mandatory = $true)][string]$What
    )

    $expectedHashes = Get-TreeHashes $Expected
    $actualHashes = Get-TreeHashes $Actual
    $differences = @(
        @($expectedHashes.Keys | Where-Object { -not $actualHashes.Contains($_) } | ForEach-Object { "missing: $_" }) +
        @($actualHashes.Keys | Where-Object { -not $expectedHashes.Contains($_) } | ForEach-Object { "extra: $_" }) +
        @($expectedHashes.Keys | Where-Object { $actualHashes.Contains($_) -and $actualHashes[$_] -ne $expectedHashes[$_] } | ForEach-Object { "differs: $_" })
    )
    if ($differences.Count -gt 0) {
        throw "$What does not match a fresh vibe init:`n  $($differences -join "`n  ")"
    }
}

function Assert-Unchanged {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Hash,
        [Parameter(Mandatory = $true)][string]$What
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Hash) {
        throw "vibe update --infrastructure changed $What, which it promises to leave alone: $Path"
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

New-ConsumerTemplate -Path $templatePath -What "Server" -NewArguments @("--interactivity", "Server")

if (-not (Test-Path -LiteralPath (Join-Path $templatePath "Vibe/Base/VibeComponent.cs") -PathType Leaf)) {
    throw "vibe init exited 0 but did not copy the infrastructure into $templatePath."
}

$appsRoot = Join-Path $WorkRoot "apps"
$apps = [System.Collections.Generic.List[object]]::new()
foreach ($componentName in $validatedNames) {
    $apps.Add([pscustomobject]@{
        Name          = $componentName
        Components    = @($componentName)
        Template      = $templatePath
        ComponentsDir = "Components/vibe"
        Project       = "Consumer.csproj"
        Assembly      = "bin/Release/net10.0/Consumer.dll"
    })
}

if (-not $isSubset) {
    $apps.Add([pscustomobject]@{
        Name          = $allTogetherName
        Components    = @($validatedNames)
        Template      = $templatePath
        ComponentsDir = "Components/vibe"
        Project       = "Consumer.csproj"
        Assembly      = "bin/Release/net10.0/Consumer.dll"
    })

    # The hosted shape: `vibe init` picks the WebAssembly client, so the components are
    # compiled there. The server project references the client, so the server assembly
    # existing means the client compiled first.
    $wasmTemplatePath = Join-Path $WorkRoot "template-wasm"
    New-ConsumerTemplate -Path $wasmTemplatePath -What "WebAssembly" -NewArguments @("--interactivity", "WebAssembly", "--all-interactive")

    if (-not (Test-Path -LiteralPath (Join-Path $wasmTemplatePath "Consumer.Client/Vibe/Base/VibeComponent.cs") -PathType Leaf)) {
        throw "vibe init exited 0 but did not copy the infrastructure into the client project of $wasmTemplatePath."
    }

    $apps.Add([pscustomobject]@{
        Name          = $allTogetherWasmName
        Components    = @($validatedNames)
        Template      = $wasmTemplatePath
        ComponentsDir = "Consumer.Client/Components/vibe"
        Project       = "Consumer/Consumer.csproj"
        Assembly      = "Consumer/bin/Release/net10.0/Consumer.dll"
    })
}

# The FluentValidation adapter, in both shapes. No components are added.
$fvTemplatePath = Join-Path $WorkRoot "template-fv"
New-ConsumerTemplate -Path $fvTemplatePath -What "Server, FluentValidation" -NewArguments @("--interactivity", "Server") -InitArguments @("--with-fluentvalidation")
Add-FluentValidationFixture -ComponentsProject $fvTemplatePath -PagesDirectory (Join-Path $fvTemplatePath "Components/Pages") -ProgramFiles @(Join-Path $fvTemplatePath "Program.cs")
$apps.Add([pscustomobject]@{
    Name          = $fluentValidationName
    Components    = @()
    Template      = $fvTemplatePath
    ComponentsDir = "Components/vibe"
    Project       = "Consumer.csproj"
    Assembly      = "bin/Release/net10.0/Consumer.dll"
})

# Hosted: the adapter, the validator and the page are in the client; the server prerenders the
# page, so it registers the validator too.
$fvWasmTemplatePath = Join-Path $WorkRoot "template-fvwasm"
New-ConsumerTemplate -Path $fvWasmTemplatePath -What "WebAssembly, FluentValidation" -NewArguments @("--interactivity", "WebAssembly", "--all-interactive") -InitArguments @("--with-fluentvalidation")
Add-FluentValidationFixture -ComponentsProject (Join-Path $fvWasmTemplatePath "Consumer.Client") -PagesDirectory (Join-Path $fvWasmTemplatePath "Consumer.Client/Pages") -ProgramFiles @(
    (Join-Path $fvWasmTemplatePath "Consumer.Client/Program.cs"),
    (Join-Path $fvWasmTemplatePath "Consumer/Program.cs")
)
$apps.Add([pscustomobject]@{
    Name          = $fluentValidationWasmName
    Components    = @()
    Template      = $fvWasmTemplatePath
    ComponentsDir = "Consumer.Client/Components/vibe"
    Project       = "Consumer/Consumer.csproj"
    Assembly      = "Consumer/bin/Release/net10.0/Consumer.dll"
})

# vibe update --infrastructure on a damaged Server project. Components are added before the
# damage, because the components are what the refreshed infrastructure has to compile under.
$updTemplatePath = Join-Path $WorkRoot "template-upd"
New-ConsumerTemplate -Path $updTemplatePath -What "Server, update" -NewArguments @("--interactivity", "Server")
foreach ($componentName in @("input", "select", "dialog")) {
    $exitCode = Invoke-Logged $ToolExecutable @("add", $componentName, "--yes", "--path", $updTemplatePath) $setupLog
    Assert-Step $exitCode "vibe add $componentName (update)" $setupLog
}

$updComponents = Join-Path $updTemplatePath "Components/vibe"
$componentHashes = Get-TreeHashes $updComponents
if ($componentHashes.Count -lt 3) {
    throw "Expected the update app to have components before the refresh, found $($componentHashes.Count)."
}

$updBaseCss = Join-Path $updTemplatePath "wwwroot/css/vibe-base.css"
Add-Content -LiteralPath $updBaseCss -Value "/* Color scheme: kept by the vendoring gate */"
$updBaseCssHash = (Get-FileHash -LiteralPath $updBaseCss -Algorithm SHA256).Hash

$updVibe = Join-Path $updTemplatePath "Vibe"
Remove-Item -LiteralPath (Join-Path $updVibe "Metadata") -Recurse -Force
Remove-Item -LiteralPath (Join-Path $updVibe "Components") -Recurse -Force
Set-Content -LiteralPath (Join-Path $updVibe "Base/VibeComponent.cs") -Value "// replaced by the vendoring gate" -Encoding utf8NoBOM

$updConfigPath = Join-Path $updTemplatePath "vibe.json"
$updConfig = Get-Content -LiteralPath $updConfigPath -Raw | ConvertFrom-Json
$updConfig.PSObject.Properties.Remove("infrastructureVersion")
$updConfig | ConvertTo-Json | Set-Content -LiteralPath $updConfigPath -Encoding utf8NoBOM

$exitCode = Invoke-Logged $ToolExecutable @("update", "--infrastructure", "--yes", "--path", $updTemplatePath) $setupLog
Assert-Step $exitCode "vibe update --infrastructure" $setupLog

Assert-SameTree -Expected (Join-Path $templatePath "Vibe") -Actual $updVibe -What "Vibe/ after vibe update --infrastructure"
foreach ($relative in $componentHashes.Keys) {
    Assert-Unchanged -Path (Join-Path $updComponents $relative) -Hash $componentHashes[$relative] -What "a component"
}
Assert-Unchanged -Path $updBaseCss -Hash $updBaseCssHash -What "the colour in vibe-base.css"
$updConfigAfter = Get-Content -LiteralPath $updConfigPath -Raw | ConvertFrom-Json
$recordedVersion = $updConfigAfter.PSObject.Properties["infrastructureVersion"]
if (-not $recordedVersion -or -not $recordedVersion.Value -or $updConfigAfter.componentsDirectory -ne "Components/vibe") {
    throw "vibe update --infrastructure did not record its version, or changed the components directory, in $updConfigPath."
}

$apps.Add([pscustomobject]@{
    Name          = $updateName
    Components    = @()
    Template      = $updTemplatePath
    ComponentsDir = "Components/vibe"
    Project       = "Consumer.csproj"
    Assembly      = "bin/Release/net10.0/Consumer.dll"
})

# vibe update --infrastructure --with-fluentvalidation on a hosted app initialised without the
# adapter, from the solution root. Its client must end up as a fresh `init --with-fluentvalidation`.
$updWasmTemplatePath = Join-Path $WorkRoot "template-updwasm"
New-ConsumerTemplate -Path $updWasmTemplatePath -What "WebAssembly, update" -NewArguments @("--interactivity", "WebAssembly", "--all-interactive")
$exitCode = Invoke-Logged $ToolExecutable @("update", "--infrastructure", "--with-fluentvalidation", "--yes", "--path", $updWasmTemplatePath) $setupLog
Assert-Step $exitCode "vibe update --infrastructure --with-fluentvalidation" $setupLog
Assert-SameTree -Expected (Join-Path $fvWasmTemplatePath "Consumer.Client/Vibe") -Actual (Join-Path $updWasmTemplatePath "Consumer.Client/Vibe") -What "the client's Vibe/ after vibe update --infrastructure --with-fluentvalidation"

Add-FluentValidationFixture -ComponentsProject (Join-Path $updWasmTemplatePath "Consumer.Client") -PagesDirectory (Join-Path $updWasmTemplatePath "Consumer.Client/Pages") -ProgramFiles @(
    (Join-Path $updWasmTemplatePath "Consumer.Client/Program.cs"),
    (Join-Path $updWasmTemplatePath "Consumer/Program.cs")
)
$apps.Add([pscustomobject]@{
    Name          = $updateWasmName
    Components    = @()
    Template      = $updWasmTemplatePath
    ComponentsDir = "Consumer.Client/Components/vibe"
    Project       = "Consumer/Consumer.csproj"
    Assembly      = "Consumer/bin/Release/net10.0/Consumer.dll"
})

$appNames = @($apps | ForEach-Object { $_.Name })

Write-Host "Vendoring $($validatedNames.Count) components, one app each..."
$vendoringFailures = @($apps | ForEach-Object -ThrottleLimit 8 -Parallel {
    $appPath = Join-Path $using:appsRoot $_.Name
    New-Item -ItemType Directory -Force -Path $appPath | Out-Null
    Copy-Item -Path (Join-Path $_.Template '*') -Destination $appPath -Recurse -Force

    $logPath = Join-Path $appPath "vibe-add.log"
    foreach ($componentName in $_.Components) {
        & $using:ToolExecutable add $componentName.ToLowerInvariant() --yes --path $appPath *>> $logPath
        if ($LASTEXITCODE -ne 0) {
            return "vibe add $componentName exited $LASTEXITCODE in app '$($_.Name)'. Log: $logPath"
        }

        # Exit 0 only says the tool did not crash; the component file is the receipt.
        $componentFile = Join-Path $appPath "$($_.ComponentsDir)/$componentName.razor"
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
$projectItems = @($apps | ForEach-Object {
    $projectPath = Join-Path (Join-Path $appsRoot $_.Name) $_.Project
    if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
        throw "Generated app '$($_.Name)' has no project at $projectPath."
    }

    "    <ConsumerApp Include=`"$([System.Security.SecurityElement]::Escape($projectPath))`" />"
}) -join [System.Environment]::NewLine
Set-Content -LiteralPath $traversalProject -Encoding utf8NoBOM -Value @"
<Project>
  <ItemGroup>
$projectItems
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

$separator = [System.IO.Path]::DirectorySeparatorChar
$results = foreach ($app in $apps) {
    $appPath = Join-Path $appsRoot $app.Name
    $assembly = Join-Path $appPath $app.Assembly

    # An error line ends with the project it came from, in brackets. Matching on the app's
    # directory attributes errors from either project of the hosted app.
    $marker = "$separator" + "apps$separator$($app.Name)$separator"
    $appErrors = @($errorLines | Where-Object { $_.Substring($_.LastIndexOf('[') + 1).Contains($marker) })

    [pscustomobject]@{
        Name   = $app.Name
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
$scopeDescription = "$($validatedNames.Count) components one at a time" +
    $(if (-not $isSubset) { ", plus all of them together in a Server app and in a WebAssembly client" } else { "" }) +
    ", plus the FluentValidation adapter in a Server app and in a WebAssembly client" +
    ", plus vibe update --infrastructure on a damaged Server app and adding the adapter to a WebAssembly client"
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
    Write-Host "Vendoring gate FAILED: $($failed.Count) of $($results.Count) apps did not compile ($scopeDescription). Excluded: $($excludedNames.Count). Registry: $($registryNames.Count)."
    Write-Host "Failed: $($failed.Name -join ', ')"
    throw "Vendoring gate failed for $($failed.Count) of $($results.Count) apps."
}

Write-Host "Vendoring gate passed: $($passed.Count) of $($results.Count) apps compiled ($scopeDescription). Excluded: $($excludedNames.Count). Registry: $($registryNames.Count)."
