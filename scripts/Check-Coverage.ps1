#Requires -Version 7.0
<#
.SYNOPSIS
    Fails when line coverage is below the agreed minimum.

.DESCRIPTION
    Reads the merged Cobertura report written by ReportGenerator and checks the line
    rate of the total and of every assembly against -Minimum.

    Both are checked on purpose. A total alone stays green while one assembly rots,
    because a large well-tested assembly averages a small untested one away.

    The merged report is the input, not the files the collector writes. Every test
    project emits its own file, the files overlap, and a line counts as covered if any
    test project reaches it. ReportGenerator unions those hits. Reading one file per
    assembly undercounts: on 2026-11-13 the best single file put
    PrivacyScanner.Infrastructure at 27.6 %, the merged report at 38.0 %.

    -RequiredAssembly names the assemblies that must appear in the report. An assembly
    that no test project loads is simply absent from it, and an absent assembly cannot
    fail a percentage check. That is how PrivacyScanner.UI, more than half of the
    coverable lines, stayed outside the figure until November 2026.

.PARAMETER ReportPath
    The merged Cobertura.xml.

.PARAMETER Minimum
    The required line rate in percent, for the total and for every assembly.

.PARAMETER RequiredAssembly
    Assembly names that must be present in the report. Accepts an array or one
    comma-separated string, because "pwsh -File" cannot pass an array.

.EXAMPLE
    ./scripts/Check-Coverage.ps1 -ReportPath ./CoverageReport/Cobertura.xml
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $ReportPath,

    [ValidateRange(0, 100)]
    [double] $Minimum = 90.0,

    [string[]] $RequiredAssembly = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$invariant = [System.Globalization.CultureInfo]::InvariantCulture

$RequiredAssembly = @($RequiredAssembly | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

function Format-Rate([double] $rate) {
    # Truncated, not rounded: 89.96 must not be printed as 90.0 next to a failure.
    ([math]::Floor($rate * 10) / 10).ToString('F1', $invariant)
}

if (-not (Test-Path -LiteralPath $ReportPath)) {
    throw "No coverage report at $ReportPath. The merge step did not run, or wrote nothing."
}

[xml] $report = Get-Content -LiteralPath $ReportPath -Raw

# DocumentElement instead of $report.coverage: the Cobertura DOCTYPE puts a
# DocumentType node beside the root, and dot notation does not resolve past it.
$root = $report.DocumentElement

$total = [double]::Parse($root.GetAttribute('line-rate'), $invariant) * 100

$assemblies = @(
    foreach ($package in $root.SelectNodes('packages/package')) {
        [pscustomobject]@{
            Name = $package.GetAttribute('name')
            Rate = [double]::Parse($package.GetAttribute('line-rate'), $invariant) * 100
        }
    }
)

if ($assemblies.Count -eq 0) {
    throw "The coverage report at $ReportPath lists no assemblies. Something upstream wrote an empty report."
}

$failures = [System.Collections.Generic.List[string]]::new()

foreach ($name in $RequiredAssembly) {
    if ($assemblies.Name -notcontains $name) {
        $failures.Add("$name is missing from the coverage report. No test project loads it, so none of its lines are measured.")
    }
}

$rows = @($assemblies | Sort-Object Rate) + [pscustomobject]@{ Name = 'TOTAL'; Rate = $total }

foreach ($row in $rows) {
    if ($row.Rate -lt $Minimum) {
        $failures.Add("$($row.Name) is at $(Format-Rate $row.Rate) %, below the required $($Minimum.ToString('0.#', $invariant)) %.")
    }
}

'Line coverage'
'-------------'
foreach ($row in $rows) {
    $mark = if ($row.Rate -lt $Minimum) { 'FAIL' } else { 'ok  ' }
    '{0}  {1,-52} {2,6} %' -f $mark, $row.Name, (Format-Rate $row.Rate)
}
''

# On GitHub Actions the same table goes to the run summary, so the figures are
# readable without opening the log.
if ($env:GITHUB_STEP_SUMMARY) {
    $summary = [System.Collections.Generic.List[string]]::new()
    $summary.Add("### Line coverage gate (minimum $($Minimum.ToString('0.#', $invariant)) %)")
    $summary.Add('')
    $summary.Add('| | Assembly | Line coverage |')
    $summary.Add('|---|---|---:|')
    foreach ($row in $rows) {
        $mark = if ($row.Rate -lt $Minimum) { 'FAIL' } else { 'ok' }
        $summary.Add("| $mark | ``$($row.Name)`` | $(Format-Rate $row.Rate) % |")
    }
    $summary.Add('')
    Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summary
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        # The workflow command puts the reason on the run page instead of only in the log.
        if ($env:GITHUB_ACTIONS) { "::error title=Coverage gate::$failure" } else { "FAILED: $failure" }
    }

    exit 1
}

"All assemblies and the total are at or above $($Minimum.ToString('0.#', $invariant)) %."
