# Exports the version property of package.json at the given path to a Version.txt file and deletes the package.json.
# The package.json.meta file is used as Version.txt.meta.
function global:Export-VersionTxt
{
    Param(
        [Parameter(Mandatory)]
        [ValidateScript({$(Test-Path $_ -PathType Container) -and $(Test-Path $(Join-Path $_ "package.json") -PathType Leaf)}, ErrorMessage = "Path provided must exist and contain a package.json file.")]
        [string]
        $PackagePath
    )

    Write-Output "Exporting version to version.txt and deleting package.json at: $PackagePath"
    Push-Location $PackagePath
    (Get-Content package.json) | ConvertFrom-Json | Select-Object -ExpandProperty "version" > Version.txt # Generate version.txt using version from package.json
    Remove-Item package.json
    # Reuse package.json.meta for the generated Version.txt so that the GUID is persistent which is better than generating a new GUID at it won't change between versions arbitrarily.
    # It may cause issues if a user switches between consuming via UPM and .unitypackages as a different asset is referenced by this GUID. Unlikely to cause any issues.
    Move-Item package.json.meta Version.txt.meta
    Get-ChildItem
    Pop-Location
}

# Exports a unitypackage at the package root path with the given ImportPath when importing into unity.
# Optionally provide an examples subpath with trailing '~' - examples will be exported as a second unitypackage.
# Example:
#  Export-UnityPackage "Packages/MyPackage" "Assets/ThirdParty" "MyPackage.unitypackage" "Examples~"
#  This will create bundle the content of "Packages/MyPackage" into the output package file "MyPackage.unitypackage".
#  The generated package that unpacks to "Assets/ThirdParty".
#  The hidden folder "Packages/MyPackages/Examples~" will be exported as a second unitypackage "MyPackage Examples.unitypackage" and will unpack to "Assets/ThirdParty/Examples".
function global:Export-UnityPackage
{
    Param(
        [Parameter(Mandatory)][string] $PackageRootPath,
        [Parameter(Mandatory)][string] $PackageImportPath,
        [Parameter(Mandatory)][string] $PackageOutputPath,
        [ValidateScript({$($(Test-Path $(Join-Path $PackageRootPath $_) -PathType Container) -and $_.EndsWith('~'))}, ErrorMessage = "If provided, example path must be an existing subdirectory hidden with a trailing '~'")]
        [string[]]
        $ExamplesSubPaths = $null
    )
    function Export-UnityPackage-Impl
    {
        param(
            [Parameter(Mandatory)]
            [ValidateScript({Test-Path $_ -PathType Container}, ErrorMessage = "Path not found or is not a directory")]
            [string]
            $ExportPath,
            [Parameter(Mandatory)]
            [ValidateNotNullOrEmpty()]
            [string]
            $ImportPath,
            [Parameter(Mandatory)]
            [ValidateNotNullOrEmpty()]
            [string]
            $Output)

        function FormatRelativePath
        {
            param(
                [Parameter(Mandatory)]
                [ValidateNotNullOrEmpty()]
                [string]
                $Path)

            # Also replaces backslashes with forward slashes and removes leading "./"
            return $($(Resolve-Path -Relative $Path) -replace '\\', '/').Substring(2)
        }

        Write-Host "Exporting '$ExportPath' to '$Output' at import path '$ImportPath'"
        
        $ExportPath = FormatRelativePath $ExportPath
        $outPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
        $tmpdir = "$([IO.Path]::GetTempPath())unitypackage"
        Remove-Item -Recurse -Force $tmpdir -ErrorAction SilentlyContinue | Out-Null
        mkdir $tmpdir -Force | Out-Null
        
        Function RecursivelyFilterSubDirectories($directoryPath) {
            $SubDirectories = Get-ChildItem $directoryPath -Directory |
                            # Filter hidden assets as described here https://docs.unity3d.com/Manual/SpecialFolders.html
                            Where-Object { -not ($_.Name.EndsWith('~') -or $_.Name.StartsWith('.') -or $_.Name -eq "cvs") }
            # Return these directories as well as all further subdirectories filtered
            # @() is used to force the result to be treated as an array
            @($SubDirectories) + @($SubDirectories | ForEach-Object { RecursivelyFilterSubDirectories($_.FullName) })
        }
        
        $PackageDirectories = @(Get-Item $ExportPath) + $(RecursivelyFilterSubDirectories($ExportPath))
        
        $Files = $PackageDirectories |
                ForEach-Object { Get-ChildItem $_.FullName -File } |
                Where-Object { 
                    $Name = [IO.Path]::GetFileNameWithoutExtension($_.Name)
                    # Filter hidden assets as described here https://docs.unity3d.com/Manual/SpecialFolders.html
                    -not ($Name.EndsWith('~') -or $_.Name.StartsWith('.') -or $Name -eq "cvs" -or $_.Extension -eq ".tmp")
                }

        
        $AnyErrors = $false
        
        $Files |
            Where-Object{ $_.Extension -ne '.meta' } | # skip meta files
            ForEach-Object{
                $FilePath = FormatRelativePath $_.FullName

                # replace relative ExportPath with ImportPath
                $FixedImportPath = $FilePath.Replace($ExportPath, $ImportPath)
                if (-not $FixedImportPath.StartsWith($ImportPath)) {
                    Write-Error "$FixedImportPath does not start with $ImportPath"
                    $AnyErrors = $true
                    return
                }
        
                $MetaFilePath = "$FilePath.meta"
                if (-not (Test-Path $MetaFilePath -PathType Leaf)) { Write-Error "Meta file not found: $MetaFilePath"; $AnyErrors = $true; return }
        
                $MatchSuccess = $(Get-Content $MetaFilePath -Raw) -match 'guid:\s*([0-9A-Fa-f]{32})\s'
                if (-not $MatchSuccess) { Write-Error "Meta file does not contain GUID or GUID is malformed: $MetaFilePath"; $AnyErrors = $true; return}
                $guid = $Matches[1]
                mkdir "$tmpdir/$guid" | Out-Null
                Copy-Item $_.FullName "$tmpdir/$guid/asset" | Out-Null
                Copy-Item $MetaFilePath "$tmpdir/$guid/asset.meta" | Out-Null
                [IO.File]::WriteAllText("$tmpdir/$guid/pathname", $FixedImportPath)
                Write-Host "Processed: $FixedImportPath"
            }
        
        if ($AnyErrors) {
            Write-Host "Skipping package generation '$Output' due to errors - please fix and try again"
            return
        }
        
        Push-Location $tmpdir
        tar -czf $outPath *
        Pop-Location

        Write-Host "Exported $outPath"
    }

    Export-UnityPackage-Impl -ExportPath $PackageRootPath -ImportPath $PackageImportPath -Output $PackageOutputPath

    if ($ExamplesSubPaths)
    {
		foreach ($ExamplesSubPath in $ExamplesSubPaths)
		{
			$ExamplesUnhiddenSubPath = $ExamplesSubPath.TrimEnd('~')
			$ExamplesHiddenPath = Join-Path $PackageRootPath $ExamplesSubPath
			$ExamplesRootPath = Join-Path $PackageRootPath $ExamplesUnhiddenSubPath

			Move-Item $ExamplesHiddenPath $ExamplesRootPath

			$ExamplesImportPath = Join-Path $PackageImportPath $ExamplesUnhiddenSubPath

			Export-UnityPackage-Impl -ExportPath $ExamplesRootPath -ImportPath $ExamplesImportPath -Output $ExamplesOutputPath
		}
    }
	# loop through list of samples and generate example packages
}