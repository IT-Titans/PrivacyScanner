#Requires -Version 7.0
<#
.SYNOPSIS
    Writes the release notes for a version from the commit subjects since the last one.

.DESCRIPTION
    Pull requests are squash-merged and their titles are checked to be Conventional
    Commits, so every commit subject on main says what kind of change it is. This
    script groups the subjects between the previous release tag and the given commit:
    breaking changes first, then features, fixes and performance, then everything else.

    Subjects that are not Conventional Commits are listed under "Other changes" and
    not dropped. The history before the title check contains many of them, and a
    change that is missing from the notes is worse than one in the wrong section.

    Merge commits are skipped; the commits they brought in are listed themselves.

    The notes end with how to verify the download and with the fact that the
    executable is not code-signed.

.PARAMETER Version
    The version being released, without the leading v.

.PARAMETER Commit
    The commit being released. Defaults to HEAD.

.PARAMETER AssetName
    The file name of the executable attached to the release.

.PARAMETER OutputPath
    Where to write the notes. Without it the notes go to the pipeline.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Version,

    [string] $Commit = 'HEAD',

    [Parameter(Mandatory)]
    [string] $AssetName,

    [string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# The newest release tag strictly before the commit. The glob is loose, the regex
# below is exact.
$previousTag = $null
$candidates = @(& git tag --list 'v[0-9]*' --merged "$Commit" --sort=-version:refname)
if ($LASTEXITCODE -ne 0) { throw "git tag failed for $Commit." }

foreach ($candidate in $candidates) {
    if ($candidate -cmatch '^v\d+\.\d+\.\d+$' -and $candidate -ne "v$Version") {
        $previousTag = $candidate
        break
    }
}

$range = if ($previousTag) { "$previousTag..$Commit" } else { $Commit }

# %x09 is a tab: a subject may contain any other separator.
$log = @(& git log --no-merges '--pretty=format:%h%x09%s' $range)
if ($LASTEXITCODE -ne 0) { throw "git log failed for $range." }

$sections = [ordered]@{
    'Breaking changes' = [System.Collections.Generic.List[string]]::new()
    'Features'         = [System.Collections.Generic.List[string]]::new()
    'Fixes'            = [System.Collections.Generic.List[string]]::new()
    'Performance'      = [System.Collections.Generic.List[string]]::new()
    'Maintenance'      = [System.Collections.Generic.List[string]]::new()
    'Other changes'    = [System.Collections.Generic.List[string]]::new()
}

$conventional = '^(?<type>build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\((?<scope>[^)]+)\))?(?<breaking>!)?: (?<summary>.+)$'

foreach ($line in $log) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }

    $hash, $subject = $line -split "`t", 2

    if ($subject -cmatch $conventional) {
        $scope = if ($Matches['scope']) { "**$($Matches['scope'])**: " } else { '' }
        $entry = "- $scope$($Matches['summary']) ($hash)"

        $section = switch ($Matches['type']) {
            'feat' { 'Features' }
            'fix' { 'Fixes' }
            'perf' { 'Performance' }
            default { 'Maintenance' }
        }

        if ($Matches['breaking']) { $section = 'Breaking changes' }

        $sections[$section].Add($entry)
    }
    else {
        $sections['Other changes'].Add("- $subject ($hash)")
    }
}

$notes = [System.Collections.Generic.List[string]]::new()

if ($previousTag) {
    $notes.Add("Changes since $previousTag.")
}
else {
    $notes.Add('First versioned release. The list below covers the whole history of the repository.')
}

foreach ($name in $sections.Keys) {
    if ($sections[$name].Count -eq 0) { continue }

    $notes.Add('')
    $notes.Add("## $name")
    $notes.Add('')
    $notes.AddRange($sections[$name])
}

$notes.Add('')
$notes.Add('## Verify the download')
$notes.Add('')
$notes.Add("``$AssetName`` is a self-contained Windows x64 executable. It is **not code-signed**, so Windows SmartScreen will warn on first start.")
$notes.Add('')
$notes.Add("Compare the SHA-256 of the file you downloaded with the value in ``$AssetName.sha256``:")
$notes.Add('')
$notes.Add('```powershell')
$notes.Add("(Get-FileHash '$AssetName' -Algorithm SHA256).Hash.ToLowerInvariant()")
$notes.Add('```')
$notes.Add('')
$notes.Add('SpaCy-based detection needs a local Python setup, described in `install.md`. Regex-based scanning works without it.')

if ($OutputPath) {
    # Line feeds and no byte order mark: the file is read on Linux in the publish job.
    [System.IO.File]::WriteAllText($OutputPath, (($notes -join "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))
    "Wrote $($notes.Count) lines to $OutputPath"
}
else {
    $notes
}
