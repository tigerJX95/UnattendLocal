$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../Tools/ReleaseAssets.ps1')
$testState = @{ Checks=0; Assets=@(); Calls=@(); AssetReads=0; AddOnRead=0; Fail=$false; BadJson=$false; FailPost=0; Posts=0 }
$release = [pscustomobject]@{ id=101; tag_name='v1.22'; draft=$false; immutable=$false }
$previousExitValue = 0
if (Get-Variable LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue) { $previousExitValue = $global:LASTEXITCODE }
function Assert-Release($value, $message) { if (-not $value) { throw $message }; $testState.Checks++ }
function Expect-ReleaseFailure([scriptblock]$Action, [string]$Message) {
    $failed=$false; try { & $Action | Out-Null } catch { $failed=$true }
    Assert-Release $failed $Message
}
function Reset-ReleaseTest {
    $testState.Assets=@(); $testState.Calls=@(); $testState.AssetReads=0; $testState.AddOnRead=0
    $testState.Fail=$false; $testState.BadJson=$false; $testState.FailPost=0; $testState.Posts=0
    $release.id=101; $release.tag_name='v1.22'; $release.draft=$false; $release.immutable=$false
}
function New-TestAsset([string]$Name, [long]$Size=10, [string]$State='uploaded') {
    [pscustomobject]@{ name=$Name; size=$Size; state=$State }
}
function gh {
    $cli=@($args); $testState.Calls+=, $cli; $global:LASTEXITCODE=0
    if ($testState.Fail) { $global:LASTEXITCODE=1; return '' }
    if ($testState.BadJson) { return '{broken' }
    if ($cli -contains 'POST') {
        $testState.Posts++
        if ($testState.FailPost -eq $testState.Posts) { $global:LASTEXITCODE=1; return '' }
        Assert-Release ($cli -contains '--input' -and $cli -notcontains '--clobber') 'Upload must read a local file and never replace assets'
        $url=$cli | Where-Object { $_ -like 'https://uploads.github.com/*' }
        Assert-Release ($url -like 'https://uploads.github.com/repos/test/project/releases/101/assets?name=*') 'Upload must use the fixed release ID'
        $file=Get-Item -LiteralPath $cli[[array]::IndexOf($cli,'--input')+1]
        $asset=New-TestAsset $file.Name $file.Length
        $testState.Assets+=,$asset
        return ($asset | ConvertTo-Json -Compress)
    }
    if ($cli[1] -like '*/assets?*') {
        Assert-Release ($cli -contains '--paginate' -and $cli -contains '--slurp') 'Read all asset pages'
        $testState.AssetReads++
        if ($testState.AddOnRead -eq $testState.AssetReads) { $testState.Assets+=,(New-TestAsset 'manual.exe') }
        $all=@($testState.Assets)
        if ($all.Count -gt 100) {
            return ('[' + (ConvertTo-Json -InputObject @($all[0..99]) -Compress) + ',' + (ConvertTo-Json -InputObject @($all[100..($all.Count-1)]) -Compress) + ']')
        }
        return ('[' + (ConvertTo-Json -InputObject $all -Compress) + ']')
    }
    return ($release | ConvertTo-Json -Compress)
}
$root=Join-Path ([IO.Path]::GetTempPath()) ('UnattendLocal-release-tests-'+[guid]::NewGuid().ToString('N'))
try {
    New-Item -ItemType Directory -Path $root | Out-Null
    foreach($variant in 'SelfContained','FrameworkDependent') { [IO.File]::WriteAllBytes((Join-Path $root "UnattendLocal_V1.22_x64_$variant.exe"), [byte[]](1,2,3)) }
    Reset-ReleaseTest
    Assert-Release (Get-ReleaseBuildDecision 'test/project' 101 'v1.22').Build 'No EXE must request a build'
    foreach($name in 'manual.exe','MANUAL.EXE','UnattendLocal_V1.22_x64_SelfContained.exe') {
        Reset-ReleaseTest; $testState.Assets=@(New-TestAsset $name)
        Assert-Release (-not (Get-ReleaseBuildDecision 'test/project' 101 'v1.22').Build) 'Any single uploaded EXE must skip compilation'
        Assert-Release ($testState.Posts -eq 0) 'Checking attachments must be read-only'
    }
    Reset-ReleaseTest; $testState.Assets=@(New-TestAsset 'source.zip')
    Assert-Release (Get-ReleaseBuildDecision 'test/project' 101 'v1.22').Build 'Non-EXE assets must not block a build'
    Reset-ReleaseTest; $testState.Assets=@(1..100 | ForEach-Object { New-TestAsset "doc$_.txt" }); $testState.Assets+=,(New-TestAsset 'later.exe')
    Assert-Release (-not (Get-ReleaseBuildDecision 'test/project' 101 'v1.22').Build) 'EXE on a later page must skip compilation'
    foreach($asset in @((New-TestAsset 'bad.exe' 0), (New-TestAsset 'partial.exe' 20 'starter'))) {
        Reset-ReleaseTest; $testState.Assets=@($asset)
        Expect-ReleaseFailure { Get-ReleaseBuildDecision 'test/project' 101 'v1.22' } 'Incomplete EXE must stop rather than compile'
    }
    foreach($kind in 'Fail','BadJson','draft','immutable','tag','id') {
        Reset-ReleaseTest
        switch($kind) { 'Fail' {$testState.Fail=$true}; 'BadJson' {$testState.BadJson=$true}; 'draft' {$release.draft=$true}; 'immutable' {$release.immutable=$true}; 'tag' {$release.tag_name='v9.9'}; 'id' {$release.id=102} }
        Expect-ReleaseFailure { Get-ReleaseBuildDecision 'test/project' 101 'v1.22' } 'API or identity failure must stop automatic compilation'
        Assert-Release ($testState.Posts -eq 0) 'Preflight error must never upload'
    }
    Reset-ReleaseTest
    foreach($tag in 'v1.22;echo bad','../v1.22','v1.22-pre1') { Expect-ReleaseFailure { Get-ReleaseBuildDecision 'test/project' 101 $tag } 'Reject invalid version tag' }
    Assert-Release ($testState.Calls.Count -eq 0) 'Invalid identifiers must not reach GitHub'
    Reset-ReleaseTest
    Publish-ReleaseExecutables 'test/project' 101 'v1.22' $root
    Assert-Release ($testState.Posts -eq 2 -and $testState.Assets.Count -eq 2) 'No EXE must upload both build outputs'
    Assert-Release ($release.tag_name -ceq 'v1.22') 'Release metadata remains unchanged'
    Publish-ReleaseExecutables 'test/project' 101 'v1.22' $root
    Assert-Release ($testState.Posts -eq 2) 'Retry with uploaded EXEs must never overwrite them'
    Reset-ReleaseTest; $testState.Assets=@(New-TestAsset 'manual.exe')
    Publish-ReleaseExecutables 'test/project' 101 'v1.22' $root
    Assert-Release ($testState.Posts -eq 0) 'Manual EXE uploaded during compilation must skip automatic upload'
    Reset-ReleaseTest; $testState.AddOnRead=2
    Publish-ReleaseExecutables 'test/project' 101 'v1.22' $root
    Assert-Release ($testState.Posts -eq 0) 'Manual EXE racing the first upload must remain untouched'
    Reset-ReleaseTest; $testState.AddOnRead=3
    Publish-ReleaseExecutables 'test/project' 101 'v1.22' $root
    Assert-Release ($testState.Posts -eq 1) 'Manual EXE racing the second upload must stop further writes'
    Reset-ReleaseTest; $testState.FailPost=2
    Expect-ReleaseFailure { Publish-ReleaseExecutables 'test/project' 101 'v1.22' $root } 'Upload failure must be surfaced'
    Assert-Release ($testState.Assets.Count -eq 1) 'Successful first upload must not be deleted after a later failure'
    Reset-ReleaseTest
    Remove-Item -LiteralPath (Join-Path $root 'UnattendLocal_V1.22_x64_FrameworkDependent.exe')
    Expect-ReleaseFailure { Publish-ReleaseExecutables 'test/project' 101 'v1.22' $root } 'Missing output must stop before any API writes'
    Assert-Release ($testState.Calls.Count -eq 0) 'Missing output must never reach GitHub'
    Write-Host "Release attachment simulation: $($testState.Checks) checks passed. No GitHub files or Releases were changed."
} finally {
    $global:LASTEXITCODE=$previousExitValue
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
