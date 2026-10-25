# Release attachment operations only. Dot-source this file; never edits/deletes/releases tags.
Set-StrictMode -Version 2.0
function Invoke-ReleaseApi {
    param([string[]]$Arguments)
    $json = @(& gh @Arguments)
    if ($LASTEXITCODE -ne 0) { throw 'GitHub Release API request failed; refusing to build or overwrite assets.' }
    try { return (($json -join "`n") | ConvertFrom-Json) }
    catch { throw 'GitHub returned invalid Release JSON.' }
}
function Get-ReleaseSnapshot {
    param([string]$Repository, [long]$ReleaseId, [string]$Tag)
    if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$' -or $ReleaseId -le 0 -or $Tag -notmatch '^v\d+\.\d+(?:\.\d+)?$') {
        throw 'Invalid repository, release ID or version tag.'
    }
    $release = Invoke-ReleaseApi -Arguments @('api', "repos/$Repository/releases/$ReleaseId")
    if ([long]$release.id -ne $ReleaseId -or $release.tag_name -cne $Tag -or $release.draft) {
        throw 'Release identity changed or is still a draft; refusing to continue.'
    }
    $pages = @(Invoke-ReleaseApi -Arguments @('api', "repos/$Repository/releases/$ReleaseId/assets?per_page=100", '--paginate', '--slurp'))
    $assets = @($pages | ForEach-Object { foreach ($asset in $_) { $asset } })
    return [pscustomobject]@{ Release=$release; Assets=$assets }
}
function Get-ReleaseBuildDecision {
    param([string]$Repository, [long]$ReleaseId, [string]$Tag)
    $snapshot = Get-ReleaseSnapshot -Repository $Repository -ReleaseId $ReleaseId -Tag $Tag
    # Any EXE means a manual distribution exists, including a single variant or a custom filename.
    $executables = @($snapshot.Assets | Where-Object { $_.name -match '(?i)\.exe$' })
    if ($executables.Count -gt 0) {
        foreach ($asset in $executables) {
            if ($asset.state -ne 'uploaded' -or [long]$asset.size -le 0) { throw 'An EXE attachment is incomplete; finish or remove it before retrying.' }
        }
        return [pscustomobject]@{ Build=$false; Reason='EXE attachment already present; automatic compilation and upload skipped.' }
    }
    if ($snapshot.Release.PSObject.Properties['immutable'] -and $snapshot.Release.immutable) {
        throw 'This Release is immutable and cannot accept EXE attachments.'
    }
    return [pscustomobject]@{ Build=$true; Reason='No EXE attachments; build both executables and attach them to this Release.' }
}
function Publish-ReleaseExecutables {
    param([string]$Repository, [long]$ReleaseId, [string]$Tag, [string]$PackageDirectory)
    $version = $Tag.Substring(1)
    $files = @('SelfContained','FrameworkDependent' | ForEach-Object {
        $path = Join-Path $PackageDirectory "UnattendLocal_V${version}_x64_$_.exe"
        $file = Get-Item -LiteralPath $path -ErrorAction Stop
        if ($file.PSIsContainer -or $file.Length -le 0) { throw 'Missing or empty build output.' }
        $file
    })
    # A user may have uploaded an EXE while Windows was compiling. Recheck before writing.
    $decision = Get-ReleaseBuildDecision -Repository $Repository -ReleaseId $ReleaseId -Tag $Tag
    if (-not $decision.Build) { Write-Host $decision.Reason; return }
    $uploadedNames = @()
    foreach ($file in $files) {
        $snapshot = Get-ReleaseSnapshot -Repository $Repository -ReleaseId $ReleaseId -Tag $Tag
        $manual = @($snapshot.Assets | Where-Object { $_.name -match '(?i)\.exe$' -and $_.name -notin $uploadedNames })
        if ($manual.Count -gt 0) { Write-Host 'An EXE appeared during upload; keeping all existing assets and stopping automatic uploads.'; return }
        # Fixed release ID, no --clobber and no DELETE/PATCH: races fail without replacement.
        $assetName = [uri]::EscapeDataString($file.Name)
        $uploaded = Invoke-ReleaseApi -Arguments @('api','--method','POST',
            "https://uploads.github.com/repos/$Repository/releases/$ReleaseId/assets?name=$assetName",
            '-H','Content-Type: application/octet-stream','--input',$file.FullName)
        if ($uploaded.name -cne $file.Name -or $uploaded.state -ne 'uploaded' -or [long]$uploaded.size -ne $file.Length) {
            throw 'GitHub did not confirm the complete EXE upload.'
        }
        $uploadedNames += $file.Name
        Write-Host "Attached: $($file.Name)"
    }
}
