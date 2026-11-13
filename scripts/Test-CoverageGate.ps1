#Requires -Version 7.0
<#
.SYNOPSIS
    Proves that Check-Coverage.ps1 fails when it must and passes when it may.

.DESCRIPTION
    A gate that has never been seen failing is an assumption. This script feeds the
    gate small Cobertura reports with a known outcome and compares the exit code. It
    runs in the pipeline before the real check, so a change that breaks the gate
    itself turns the run red instead of silently green.

    The third case is the reason the per-assembly check exists: the total is above
    the minimum while one assembly is far below it.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$gate = Join-Path $PSScriptRoot 'Check-Coverage.ps1'
$workDir = Join-Path ([System.IO.Path]::GetTempPath()) "coverage-gate-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $workDir | Out-Null

function New-Report([string] $name, [double] $totalRate, [hashtable] $assemblies) {
    $invariant = [System.Globalization.CultureInfo]::InvariantCulture
    $packages = foreach ($assembly in $assemblies.Keys) {
        '<package name="{0}" line-rate="{1}"><classes /></package>' -f $assembly, $assemblies[$assembly].ToString($invariant)
    }
    $path = Join-Path $workDir "$name.xml"
    @(
        '<?xml version="1.0" encoding="utf-8"?>'
        '<!DOCTYPE coverage SYSTEM "http://cobertura.sourceforge.net/xml/coverage-04.dtd">'
        ('<coverage line-rate="{0}" version="0">' -f $totalRate.ToString($invariant))
        '<packages>'
        $packages
        '</packages>'
        '</coverage>'
    ) | Set-Content -LiteralPath $path -Encoding utf8
    $path
}

$cases = @(
    @{
        Name     = 'everything above the minimum passes'
        Report   = New-Report 'pass' 0.95 @{ 'A' = 0.93; 'B' = 0.97 }
        Required = 'A,B'
        Expected = 0
        Output   = 'at or above 90 %'
    }
    @{
        Name     = 'a total below the minimum fails'
        Report   = New-Report 'total-low' 0.60 @{ 'A' = 0.60; 'B' = 0.60 }
        Required = ''
        Expected = 1
        Output   = 'TOTAL is at 60.0 %'
    }
    @{
        Name     = 'one assembly below the minimum fails although the total is above it'
        Report   = New-Report 'assembly-low' 0.94 @{ 'A' = 0.99; 'B' = 0.685 }
        Required = ''
        Expected = 1
        Output   = 'B is at 68.5 %'
    }
    @{
        Name     = 'a value just below the minimum fails'
        Report   = New-Report 'edge' 0.8996 @{ 'A' = 0.8996 }
        Required = ''
        Expected = 1
        Output   = 'A is at 89.9 %'
    }
    @{
        Name     = 'a required assembly that is missing from the report fails'
        Report   = New-Report 'missing' 0.95 @{ 'A' = 0.95 }
        Required = 'A,B'
        Expected = 1
        Output   = 'B is missing from the coverage report'
    }
)

$failed = 0

try {
    foreach ($case in $cases) {
        # A child process, because the gate ends with "exit".
        $arguments = @('-NoProfile', '-File', $gate, '-ReportPath', $case.Report, '-Minimum', '90')
        if ($case.Required) { $arguments += '-RequiredAssembly', $case.Required }

        $output = (& pwsh @arguments 2>&1 | Out-String)
        $actual = $LASTEXITCODE

        # The exit code alone is not enough: a gate that fails for the wrong reason,
        # a parameter error for instance, would also exit with 1.
        if ($actual -eq $case.Expected -and $output -match $case.Output) {
            "ok    $($case.Name)"
        }
        else {
            "WRONG $($case.Name): expected exit code $($case.Expected) and output matching '$($case.Output)', got exit code $actual and:"
            $output
            $failed++
        }
    }

    # A report that does not exist must not look like a pass either.
    $output = (& pwsh -NoProfile -File $gate -ReportPath (Join-Path $workDir 'absent.xml') 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0 -and $output -match 'No coverage report at') {
        'ok    a missing report fails'
    }
    else {
        'WRONG a missing report fails: got exit code 0'
        $failed++
    }
}
finally {
    Remove-Item -LiteralPath $workDir -Recurse -Force
}

if ($failed -gt 0) {
    "$failed case(s) behaved differently than expected. The coverage gate cannot be trusted in this state."
    exit 1
}

'The coverage gate fails and passes where it should.'
