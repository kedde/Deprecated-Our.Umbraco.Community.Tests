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
$currentPath = [System.IO.Path]::GetDirectoryName($pwd)
Write-Host $currentPath
Get-ChildItem

$mpath =  $currentPath + "\Umbraco-CMS\build\Modules\"

if (-not [System.IO.Directory]::Exists($mpath + "Umbraco.Build"))
{
  Write-Error "Could not locate Umbraco build Powershell module."
  break
}

$env:PSModulePath = "$mpath;$env:PSModulePath"
Import-Module Umbraco.Build -Force -DisableNameChecking

Set-Location $mpath
Get-ChildItem



Write-Host "importing umbraco environment"
foreach ($num in 1) {
    # dirty hack because of break in build.ps1 when using -mo
    # Invoke-Expression -Verbose  -ErrorAction silentlyContinue ".\build\build.ps1 -mo" 
}

# Invoke-Expression build\build.ps1 -mo
Write-Host "Done - importing umbraco environment"
# Set-Location ..
# build 
# .\Umbraco-cms\build\build.bat

# after 7.7
$umbracoVersionVars = Get-UmbracoVersion
$umbracoVersion = $umbracoVersionVars.Release
Write-Host $umbracoVersionVars 
Write-Host "version " $umbracoVersion

# pause
# prior
# read current version
# $versionFile = ".\Umbraco-cms\build\UmbracoVersion.txt"
#$umbracoVersion = Get-Content $versionFile | Select-Object -last 1
Write-Host "updating nuspec " + $umbracoVersion

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
    # $uenv = Get-UmbracoBuildEnv
    Set-UmbracoVersion $umbracoVersion
    Set-Location .\Umbraco-CMS\build
    Build-Umbraco pre-tests Debug
    Build-Umbraco compile-tests Debug
    Set-Location ..\..

    # run bat file
    # Set-Location .\Umbraco-CMS\build
    # build-umbraco compile-tests
    # Set-Location ..\..
    # if ($LASTEXITCODE -ne 0)
    # {
    #     Write-Error "Encountered error while running build.bat"
    #     exit
    # }

    # $env:nugetPushNeeded="true"

    # update version in nuspec
    Write-Host "changing version " $versionNode.InnerText " to " $umbracoVersion
    $versionNode.InnerText = $umbracoVersion
    $doc.Save($file.FullName)
    Write-Host "version " $versionNode.InnerText 

    # # restore nuget in test directore
    # Write-Host "restore nuget packages"
    # $slnDirectory = ".\Umbraco-cms\src"
    # $projects = Get-ChildItem -path $slnDirectory -Recurse -Include *.csproj
    # foreach ($projFile in $projects)
    # {
    #     Write-Host $projFile
    #     NuGet.exe restore $projFile -solutiondirectory $slnDirectory
    # }
    
    # # # build the solution
    # Write-Host "start building the tests project"
    # $msbuild = "C:\Program` Files` (x86)\MSBuild\14.0\Bin\MSBuild.exe"

    
    # # $testProj=".\Umbraco-cms\src\Umbraco.Tests\Umbraco.Tests.csproj"
    # $sln="Umbraco-cms\src\Umbraco.sln"
    # # /t:Umbraco.Tests 
    # # $build = "&'" + $msbuild + "' " +$sln +  " /p:Configuration=Debug /consoleloggerparameters:ErrorsOnly /langversion:6"
    # $build = "&'" + $msbuild + "' " +$sln +  " /p:Configuration=Debug /t:rebuild /consoleloggerparameters:ErrorsOnly"
    # Invoke-Expression $build    
    # Write-Host "done building umbraco $($umbracoVersion)"


    # build package
    Write-Host "pack nuget package"
    nuget pack .\Our.Umbraco.Community.Tests\Package.nuspec -OutputDirectory .\Our.Umbraco.Community.Tests\

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
    #git commit -am "update version to $($umbracoVersion)"
    #git push 
    # Write-Host "deploy stuff should take over now"
}
else {
    Write-Host "no build needed"
}
