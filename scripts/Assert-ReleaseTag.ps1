#Requires -Version 7.0
<#
.SYNOPSIS
    Refuses to release from a tag that does not describe the code it points at.

.DESCRIPTION
    Three things must hold before a tag may become a release:

    1. The tag is vX.Y.Z and nothing else. No pre-release suffix, no fourth number.
    2. X.Y.Z equals the version in version.json at the tagged commit. The executable
       takes its version from version.json (Nerdbank.GitVersioning), so a tag that
       says something else would publish a v1.2.0 that reports 1.0.0 about itself.
    3. The tagged commit is part of main. Anything else has not been through a pull
       request and the pipeline.

    Prints the version on success. On GitHub Actions it also writes it to the step
    output "version".

.PARAMETER Tag
    The tag name, for example v1.2.0.

.PARAMETER Commit
    The commit the tag points at. Defaults to HEAD, which is what the release
    workflow has checked out.

.PARAMETER MainRef
    The ref that must contain the commit.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Tag,

    [string] $Commit = 'HEAD',

    [string] $MainRef = 'origin/main'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Stop-Release([string] $reason) {
    if ($env:GITHUB_ACTIONS) { "::error title=Release refused::$reason" } else { "REFUSED: $reason" }
    exit 1
}

if ($Tag -cnotmatch '^v(?<version>(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*))$') {
    Stop-Release "'$Tag' is not a release tag. Expected vX.Y.Z, for example v1.2.0."
}

$tagVersion = $Matches['version']

$declared = (& dotnet nbgv get-version $Commit --variable SimpleVersion)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($declared)) {
    Stop-Release "Could not read the version of $Commit from version.json (dotnet nbgv get-version failed). Run 'dotnet tool restore' first."
}

$declared = "$declared".Trim()

if ($declared -ne $tagVersion) {
    Stop-Release "Tag $Tag says $tagVersion, but version.json at that commit says $declared. Change version.json through a pull request first, then tag the merged commit."
}

& git merge-base --is-ancestor $Commit $MainRef
if ($LASTEXITCODE -ne 0) {
    Stop-Release "The commit behind $Tag is not part of $MainRef. Releases are cut from main only."
}

if ($env:GITHUB_OUTPUT) {
    Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "version=$tagVersion"
}

$tagVersion
