# Google Drive Image Sync Script
# Syncs images from Google Drive to Unity Assets with folder-based renaming

param(
    [string]$SourcePath = "G:\マイドライブ\協力脱出ゲーム\イラスト",
    [string]$DestPath = "C:\Users\runco\Unity\2D_Online_Escape\Assets\Images"
)

# Create destination if it doesn't exist
if (-not (Test-Path $DestPath)) {
    New-Item -ItemType Directory -Path $DestPath -Force | Out-Null
}

# Track all valid destination files for cleanup
$validDestFiles = @{}

# Files to exclude
$excludeFiles = @("desktop.ini", "Thumbs.db", ".DS_Store")

function Get-RelativePrefix {
    param([string]$RelativePath)
    
    $parts = $RelativePath -split [regex]::Escape([IO.Path]::DirectorySeparatorChar)
    # Remove the filename (last part) and join with underscore
    if ($parts.Count -gt 1) {
        $folderParts = $parts[0..($parts.Count - 2)]
        return ($folderParts -join "_") + "_"
    }
    return ""
}

function Sync-Directory {
    param(
        [string]$CurrentSource,
        [string]$RelativePath = ""
    )
    
    # Get all items in current source directory
    $items = Get-ChildItem -Path $CurrentSource -ErrorAction SilentlyContinue
    
    foreach ($item in $items) {
        # Skip excluded files
        if ($excludeFiles -contains $item.Name) {
            continue
        }
        
        $itemRelativePath = if ($RelativePath) { 
            Join-Path $RelativePath $item.Name 
        } else { 
            $item.Name 
        }
        
        if ($item.PSIsContainer) {
            # It's a directory - recurse into it
            Sync-Directory -CurrentSource $item.FullName -RelativePath $itemRelativePath
        }
        else {
            # It's a file - process it
            $prefix = Get-RelativePrefix -RelativePath $itemRelativePath
            $newFileName = $prefix + $item.Name
            $destFilePath = Join-Path $DestPath $newFileName
            
            # Track this as a valid file
            $validDestFiles[$destFilePath] = $true
            
            # Check if file needs to be copied
            $shouldCopy = $false
            
            if (-not (Test-Path $destFilePath)) {
                $shouldCopy = $true
                Write-Host "[NEW] $newFileName"
            }
            else {
                $destFile = Get-Item $destFilePath
                if ($item.LastWriteTime -gt $destFile.LastWriteTime) {
                    $shouldCopy = $true
                    Write-Host "[UPDATE] $newFileName"
                }
                else {
                    Write-Host "[SKIP] $newFileName (up to date)"
                }
            }
            
            if ($shouldCopy) {
                Copy-Item -Path $item.FullName -Destination $destFilePath -Force
            }
        }
    }
}

Write-Host "========================================="
Write-Host "Google Drive Image Sync"
Write-Host "========================================="
Write-Host "Source: $SourcePath"
Write-Host "Destination: $DestPath"
Write-Host "========================================="
Write-Host ""

# Sync files
Sync-Directory -CurrentSource $SourcePath

Write-Host ""
Write-Host "========================================="
Write-Host "Cleaning up obsolete files..."
Write-Host "========================================="

# Remove files that no longer exist in source
$existingDestFiles = Get-ChildItem -Path $DestPath -File -ErrorAction SilentlyContinue
foreach ($destFile in $existingDestFiles) {
    # Skip .meta files (Unity will handle them)
    if ($destFile.Extension -eq ".meta") {
        continue
    }
    
    if (-not $validDestFiles.ContainsKey($destFile.FullName)) {
        Write-Host "[DELETE] $($destFile.Name)"
        Remove-Item -Path $destFile.FullName -Force
        
        # Also remove the .meta file if it exists
        $metaFile = $destFile.FullName + ".meta"
        if (Test-Path $metaFile) {
            Remove-Item -Path $metaFile -Force
        }
    }
}

Write-Host ""
Write-Host "========================================="
Write-Host "Sync complete!"
Write-Host "========================================="

