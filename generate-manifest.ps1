param(
 [string]$projectPath,
 [string]$targetPath
)

$productName = Get-Content $projectPath | Select-String -Pattern '<Product>(.*?)<\/Product>' | % { $_.Matches } | % { $_.Groups[1].Value }
$versionNumber = Get-Content $projectPath | Select-String -Pattern '<Version>(.*?)<\/Version>' | % { $_.Matches } | % { $_.Groups[1].Value }
$repositoryUrl = Get-Content $projectPath | Select-String -Pattern '<RepositoryUrl>(.*?)<\/RepositoryUrl>' | % { $_.Matches } | % { $_.Groups[1].Value }
$description = Get-Content $projectPath | Select-String -Pattern '<Description>(.*?)<\/Description>' | % { $_.Matches } | % { $_.Groups[1].Value }

$manifestContent = @"
{
 "name": "$productName",
 "version_number": "$versionNumber",
 "website_url": "$repositoryUrl",
 "description": "$description",
 "dependencies": ["BepInEx-BepInExPack-5.4.2100"]
}
"@

Set-Content -Path "$targetPath\manifest.json" -Value $manifestContent