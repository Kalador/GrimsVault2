param(
    [string]$Root
)

if (-not $Root) {
    $Root = Get-Location
}

if (Test-Path $Root -PathType Leaf) {
    $Root = Split-Path $Root -Parent
}

$Root = (Resolve-Path $Root).Path

Write-Host "ROOT = $Root"

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"

# Output files (one per type)
$outCs      = Join-Path $Root "Vault2.0_cs_$stamp.txt"
$outCshtml  = Join-Path $Root "Vault2.0_cshtml_$stamp.txt"
$outJs      = Join-Path $Root "Vault2.0_js_$stamp.txt"

# Create empty files
Set-Content -Path $outCs -Value "" -Encoding UTF8
Set-Content -Path $outCshtml -Value "" -Encoding UTF8
Set-Content -Path $outJs -Value "" -Encoding UTF8

# Allowed file types
$allowedExtensions = ".cs", ".cshtml", ".js"

# Excluded folders
$excludeFolders = "bin", "obj", ".vs", "node_modules", "lib"

# Gather files
$files = Get-ChildItem -Path "$Root\*" -Recurse -File |
    Where-Object {
        $_.Extension -in $allowedExtensions -and
        -not (
            ($_.DirectoryName -split '\\') |
            Where-Object { $_ -in $excludeFolders }
        )
    }

Write-Host "FOUND FILES AFTER FILTER: $($files.Count)"

foreach ($file in $files) {

    $relative = $file.FullName.Substring($Root.TrimEnd('\').Length).TrimStart('\')

    # Read raw content exactly as-is
    $raw = [System.IO.File]::ReadAllText($file.FullName)

    switch ($file.Extension.ToLower()) {

        ".cs" {
            Add-Content -Path $outCs -Value "===== FILE: $relative =====" -Encoding UTF8
            Add-Content -Path $outCs -Value "" -Encoding UTF8
            Add-Content -Path $outCs -Value $raw -Encoding UTF8
            Add-Content -Path $outCs -Value "`r`n" -Encoding UTF8
        }

        ".cshtml" {
            Add-Content -Path $outCshtml -Value "===== FILE: $relative =====" -Encoding UTF8
            Add-Content -Path $outCshtml -Value "" -Encoding UTF8
            Add-Content -Path $outCshtml -Value $raw -Encoding UTF8
            Add-Content -Path $outCshtml -Value "`r`n" -Encoding UTF8
        }

        ".js" {
            Add-Content -Path $outJs -Value "===== FILE: $relative =====" -Encoding UTF8
            Add-Content -Path $outJs -Value "" -Encoding UTF8
            Add-Content -Path $outJs -Value $raw -Encoding UTF8
            Add-Content -Path $outJs -Value "`r`n" -Encoding UTF8
        }
    }
}

Write-Host "Export complete:"
Write-Host "  CS:      $outCs"
Write-Host "  CSHTML:  $outCshtml"
Write-Host "  JS:      $outJs"
