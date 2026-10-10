param(
    [string] $DocumentationDirectory = 'docs/_site',
    [string] $BrowserDirectory = 'artifacts/pages/browser/wwwroot',
    [string] $OutputDirectory = 'artifacts/pages/site'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$siteRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
$artifactRoot = Join-Path $repositoryRoot 'artifacts'
if (-not $siteRoot.StartsWith($artifactRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Pages output must be a directory below this repository artifacts directory.'
}
$docsRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $DocumentationDirectory))
$browserRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $BrowserDirectory))
foreach ($source in @($docsRoot, $browserRoot)) {
    if ($source -eq $siteRoot -or
        $source.StartsWith($siteRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        $siteRoot.StartsWith($source + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Pages output must not overlap either input directory.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $source 'index.html') -PathType Leaf)) {
        throw "Build output is missing: $source/index.html"
    }
}
if (Test-Path -LiteralPath $siteRoot) {
    Remove-Item -LiteralPath $siteRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $siteRoot | Out-Null
Get-ChildItem -LiteralPath $docsRoot -Force | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $siteRoot -Recurse -Force
}
$demoRoot = Join-Path $siteRoot 'demo'
New-Item -ItemType Directory -Path $demoRoot -Force | Out-Null
Get-ChildItem -LiteralPath $browserRoot -Force | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $demoRoot -Recurse -Force
}
$entry = Join-Path $demoRoot 'index.html'
$html = [IO.File]::ReadAllText($entry)
if ($html -match '<base\s') {
    throw 'The preview entry already has a base URL; review its configuration before assembling Pages.'
}
if (-not $html.Contains('<head>')) {
    throw 'The preview entry is missing its HTML head.'
}
# Relative asset URLs work at both the repository Pages path and a custom domain.
$html = $html.Replace('<head>', '<head>' + "`n" + '  <base href="./" />')
[IO.File]::WriteAllText($entry, $html)
# Published compressed entries would otherwise contain the pre-assembly HTML.
foreach ($suffix in @('.br', '.gz')) {
    [IO.File]::Delete($entry + $suffix)
}
[IO.File]::WriteAllText((Join-Path $siteRoot '.nojekyll'), '')
if (-not (Test-Path -LiteralPath (Join-Path $demoRoot '_framework') -PathType Container)) {
    throw 'The preview is missing its WebAssembly runtime assets.'
}
Write-Host "Pages site: $siteRoot (documentation at /, preview at /demo/)"
