# Script is used as a helper to re-order resw files. This helps keep them clean and organised over time.

$translationsPath = Join-Path -Path (Split-Path -Path $PSScriptRoot -Parent | Split-Path -Parent) -ChildPath 'src/Translations'
$reswFiles = Get-ChildItem -Path $translationsPath -Filter *.resw -Recurse

foreach ($file in $reswFiles)
{
    Write-Host "Sorting keys in $($file.FullName)"

    [xml]$xml = Get-Content $file.FullName   
    $dataNodes = $xml.root.data | Sort-Object { $_.name }

    # Remove each data node
    foreach ($node in $xml.root.data) {
        $xml.root.RemoveChild($node) | Out-Null
    }

    # Add each data node back in from our sorted list
    foreach ($node in $dataNodes)
    {
        $importedNode = $xml.ImportNode($node, $true)
        $xml.root.AppendChild($importedNode) | Out-Null
    }

    $xml.Save($file.FullName)
}
