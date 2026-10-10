param(
    [Parameter(Mandatory = $true)]
    [string] $Executable
)

$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $Executable).Path
$bytes = [System.IO.File]::ReadAllBytes($source)
$peOffset = [BitConverter]::ToInt32($bytes, 0x3c)
if ([BitConverter]::ToUInt32($bytes, $peOffset) -ne 0x4550 -or
    [BitConverter]::ToUInt16($bytes, $peOffset + 4) -ne 0x8664 -or
    [BitConverter]::ToUInt16($bytes, $peOffset + 24) -ne 0x20b) {
    throw 'Expected a Windows x64 PE executable.'
}
$clrDirectory = $peOffset + 24 + 112 + 14 * 8
if ([BitConverter]::ToUInt64($bytes, $clrDirectory) -ne 0) {
    throw 'The executable still contains a managed CLR header.'
}

$verificationRoot = Join-Path $PSScriptRoot '../artifacts/verification'
$isolated = Join-Path $verificationRoot ('notepad-aot-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $isolated -Force | Out-Null
$isolated = (Resolve-Path -LiteralPath $isolated).Path
$testExe = Join-Path $isolated 'Notepad.exe'
Copy-Item -LiteralPath $source -Destination $testExe
if (@(Get-ChildItem -LiteralPath $isolated -Force).Count -ne 1) {
    throw 'The startup test directory must contain only the executable.'
}

$testProcess = $null
try {
    $testProcess = Start-Process -FilePath $testExe -WorkingDirectory $isolated -WindowStyle Hidden -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds(20)
    do {
        Start-Sleep -Milliseconds 250
        $testProcess.Refresh()
        if ($testProcess.HasExited) { throw "Notepad exited during startup: $($testProcess.ExitCode)" }
    } while ($testProcess.MainWindowHandle -eq 0 -and [DateTime]::UtcNow -lt $deadline)

    if ($testProcess.MainWindowHandle -eq 0 -or !$testProcess.Responding) {
        throw 'Notepad did not create a responsive desktop window.'
    }
    $modules = @($testProcess.Modules | ForEach-Object { $_.ModuleName })
    $unexpected = @($modules | Where-Object {
        $_ -match '^(libSkiaSharp|libHarfBuzzSharp|av_libglesv2|libGLESv2|coreclr|hostfxr|hostpolicy)\.dll$'
    })
    if ($unexpected.Count -gt 0) {
        throw ('Unexpected external native/runtime modules: ' + ($unexpected -join ', '))
    }
    $report = [ordered]@{
        CheckedAtUtc = [DateTime]::UtcNow.ToString('o')
        Executable = $source
        Sha256 = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
        Bytes = (Get-Item -LiteralPath $source).Length
        IsolatedDirectory = $isolated
        PeMachine = 'x64'
        HasClrHeader = $false
        Startup = 'Passed'
        WindowTitle = $testProcess.MainWindowTitle
        Responding = $testProcess.Responding
        ExternalGraphicsOrClrModules = $unexpected
        LoadedModules = $modules
        InteractiveEditingAndDialogs = 'Not tested by this startup smoke check'
    }
    $json = $report | ConvertTo-Json -Depth 4
    $json | Set-Content -LiteralPath (Join-Path $isolated 'smoke-report.json') -Encoding utf8
    Write-Output $json
}
finally {
    if ($testProcess -and !$testProcess.HasExited) {
        Stop-Process -Id $testProcess.Id -ErrorAction SilentlyContinue
    }
}
