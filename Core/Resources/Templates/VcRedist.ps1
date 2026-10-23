# UnattendLocal template: Microsoft-signed VC++ v14 redistributables.
$ErrorActionPreference = 'Stop'
$architecture = $env:PROCESSOR_ARCHITEW6432
if (-not $architecture) { $architecture = $env:PROCESSOR_ARCHITECTURE }
$packages = switch ($architecture.ToUpperInvariant()) {
    'AMD64' { 'x86'; 'x64' }
    'ARM64' { 'x86'; 'arm64' }
    'X86' { 'x86' }
    default { throw ('Unsupported target architecture: ' + $architecture) }
}
$directory = Join-Path ([System.IO.Path]::GetTempPath()) ('UnattendLocal-VC-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -Path $directory -ItemType Directory
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    foreach ($package in $packages) {
        $file = Join-Path $directory ('vc_redist.' + $package + '.exe')
        Invoke-WebRequest -Uri ('https://aka.ms/vc14/vc_redist.' + $package + '.exe') -OutFile $file -UseBasicParsing -TimeoutSec 300
        $signature = Get-AuthenticodeSignature -LiteralPath $file
        if ($signature.Status -ne 'Valid' -or -not $signature.SignerCertificate -or
            $signature.SignerCertificate.Subject -notmatch '(?:^|,\s*)O=Microsoft Corporation(?:,|$)') {
            throw ('Refusing installer without a valid Microsoft signature: ' + $package)
        }
        $process = Start-Process -FilePath $file -ArgumentList '/install /quiet /norestart' -Wait -PassThru
        if ($process.ExitCode -notin @(0, 3010, 1638)) {
            throw ('VC++ installation failed for {0}, exit code {1}.' -f $package, $process.ExitCode)
        }
        if ($process.ExitCode -eq 3010) { Write-Warning 'VC++ installation requires a later restart.' }
        if ($process.ExitCode -eq 1638) { Write-Host 'An equal or newer VC++ runtime is already installed.' }
    }
} finally {
    Remove-Item -LiteralPath $directory -Recurse -Force -ErrorAction SilentlyContinue
}
