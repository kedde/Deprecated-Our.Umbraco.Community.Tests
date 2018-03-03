# choco install nuget.commandline?
$UmbracoCms = "Umbraco-Cms"
if (!(Test-Path -Path $UmbracoCms)) {
    git clone https://github.com/umbraco/Umbraco-CMS.git
}
else {
    Set-Location Umbraco-cms
    git pull
    Set-Location ..
}

# checkout the master
Set-Location Umbraco-Cms
git checkout master-v7
Set-Location ..

# import umbraco module
$mpath = $PSScriptRoot + "\Umbraco-CMS\build\Modules\"

if (-not [System.IO.Directory]::Exists($mpath + "Umbraco.Build")) {
    Write-Error "Could not locate Umbraco build Powershell module."
    break
}

$env:PSModulePath = "$mpath;$env:PSModulePath"
Import-Module Umbraco.Build -Force -DisableNameChecking

Write-Host "Done - importing umbraco environment"

# after 7.7
$umbracoVersionVars = Get-UmbracoVersion
$umbracoVersion = $umbracoVersionVars.Release
Write-Host $umbracoVersionVars 
Write-Host "version " $umbracoVersion


# update version number in nuspec package
$file = Get-Item ".\Our.Umbraco.Community.Tests\Package.nuspec"
Write-Host $file.FullName
[xml] $doc = Get-Content($file.FullName)
$versionNode = $doc.SelectSingleNode("//package/metadata/version")
$oldVersion = $versionNode.InnerText

# if version changed
$debug = $false
if ($oldVersion -ne $umbracoVersion -Or $debug) {
    # compile the test
    Set-UmbracoVersion $umbracoVersion
    Set-Location .\Umbraco-CMS\build
    Build-Umbraco pre-nuget Debug
    Build-Umbraco pre-build Debug
    Build-Umbraco restore-nuget Debug
    # Build-Umbraco compile-umbraco Debug
    Build-Umbraco pre-tests Debug
    Build-Umbraco compile-tests Debug
    Set-Location ..\..

    Get-Location
    # update version in nuspec
    Write-Host "updating nuspec " + $umbracoVersion
    Write-Host "changing version " $versionNode.InnerText " to " $umbracoVersion
    $versionNode.InnerText = $umbracoVersion
    $doc.Save($file.FullName)
    Write-Host "version " $versionNode.InnerText 

    # build package
    Write-Host "pack nuget package"
    nuget pack .\Our.Umbraco.Community.Tests\Package.nuspec -OutputDirectory .\Our.Umbraco.Community.Tests\

    Write-Host "current location"
    Get-Location
    Get-ChildItem

    $nupkg = "$PSScriptRoot\Our.Umbraco.Community.Tests\Our.Umbraco.Community.Tests.$($umbracoVersion).nupkg"
    if (-not [System.IO.Directory]::Exists($nupkg)) {
        Write-Error "nuget package error: $nupkg does not exist"
        Set-Location .\Our.Umbraco.Community.Tests
        Get-ChildItem
        Write-Host $nupkg
        break
    }

    Write-Host "Push-AppveyorArtifact .\Our.Umbraco.Community.Tests\Our.Umbraco.Community.Tests.$($umbracoVersion).nupkg"

    # push to nuget
    Push-AppveyorArtifact .\Our.Umbraco.Community.Tests\Our.Umbraco.Community.Tests.$($umbracoVersion).nupkg


    # push to myget
    # nuget push SamplePackage.1.0.0.nupkg <your access token> -Source https://www.myget.org/F/umbraco-packages/
    # Push-AppveyorArtifact .\Our.Umbraco.Community.Tests\Our.Umbraco.Community.Tests.$($umbracoVersion).nupkg

    # upload package
    # make sure the nuget setApiKey Your-API-Key has been executed or set in the environment
    #$apikey = $env:nugetApiKey
    # nuget.exe setApiKey $apikey -Source https://www.nuget.org/api/v2/package
    #nuget push .\kedde.Umbraco.TestsDlls\kedde.umbraco.testdlls.7.5.7.nupkg -source https://www.nuget.org/api/v2/package

    # push update version back
    # git commit -am "update version to $($umbracoVersion)"
    # git push 
    Write-Host "deploy stuff should take over now"
}
else {
    Write-Host "no build needed"
}
