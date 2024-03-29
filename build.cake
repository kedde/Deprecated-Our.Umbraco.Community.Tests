#addin nuget:?package=Cake.Git&version=2.0.0
#addin nuget:?package=semver&version=2.0.4
#addin nuget:?package=Cake.FileHelpers&version=5.0.0
#addin nuget:?package=Cake.ExtendedNuGet&version=5.0.0

var UmbracoFolder = "Umbraco-CMS";
var UmbracoSolution = UmbracoFolder + "/src/umbraco.sln";
var UmbracoTestProj = UmbracoFolder + "/src/Umbraco.Tests/Umbraco.Tests.csproj";
var UmbracoVersion = "7.10.4"; 

// .\build.ps1 -Target CloneUmbraco
// .\build.ps1 -Target CheckRemoteTagsAndBuildIfNeeded
// .\build.ps1 -Target CheckRemoteTagsAndBuildIfNeeded (default)
var target = Argument("target", "CheckRemoteTagsAndBuildIfNeeded");
var postfix = Argument("postfix", "");

Task("CloneUmbraco")
    .Does(() =>
{
    if (!DirectoryExists(UmbracoFolder))
    {
        Console.WriteLine("Cloning to Umbraco-folder ");
        GitClone("https://github.com/umbraco/Umbraco-CMS.git", UmbracoFolder);
    }
    else
    {
        var exitCodeHardReset = StartProcess("git", new ProcessSettings{ Arguments = "reset --hard", WorkingDirectory = UmbracoFolder });
        var exitCodeCheckoutMasterWithArgument = StartProcess("git", new ProcessSettings{ Arguments = " checkout main", WorkingDirectory = UmbracoFolder });
        GitPull(UmbracoFolder, "kedde", "kedde@kedde.dk");
    }

});

Task("CheckRemoteTagsAndBuildIfNeeded")
.Does(()=>{
    // git ls-remote --tags https://github.com/umbraco/Umbraco-CMS.git
    IEnumerable<string> redirectedStandardOutput;
    var exitCodeWithArgument =
     StartProcess(
         "git",
         new ProcessSettings {
             Arguments = "ls-remote --tags https://github.com/umbraco/Umbraco-CMS.git",
             RedirectStandardOutput = true
         },
         out redirectedStandardOutput
     );
     foreach (var r in redirectedStandardOutput){
         var tagSplit = r.Split('\t');
         var commitHash = tagSplit[0];
         var tagName = tagSplit[1];
         if (tagName.Contains("release-") && !tagName.EndsWith("^{}"))
         {
            Console.WriteLine("tagname: " + tagName);
            if (!tagName.Contains("release-netcore")){
                var version = tagName.Substring(tagName.IndexOf("-") + 1 );
                var firstDot = version.IndexOf(".");
                var major = version.Substring(0, firstDot);
                var secondDot = version.IndexOf(".", firstDot + 1);
                var minor = "0";
                if (secondDot > 0){
                    minor = version.Substring(firstDot + 1, secondDot - firstDot -1);
                }
                // umbraco 7
                if (int.Parse(major) == 7 && int.Parse(minor) >= 10){
                    Console.WriteLine("version: " + version + " major: " + major + " minor " + minor);

                    var versionFile = "versions.txt";
                    var matches = FindRegexMatchesInFile(versionFile, version, System.Text.RegularExpressions.RegexOptions.None);
                    if (matches.Count == 0){
                        Console.WriteLine("setting version to " + version);
                        UmbracoVersion = version;
                        RunTarget("NugetPush");
                    }
                    else{
                        Console.WriteLine("Found #" + matches.Count + " in file build not needed");
                    }
                }

                // umbraco 8
                if (int.Parse(major) == 8){
                    Console.WriteLine("version: " + version + " major: " + major + " minor " + minor);

                    var versionFile = "versions.txt";
                    var matches = FindRegexMatchesInFile(versionFile, version, System.Text.RegularExpressions.RegexOptions.None);
                    if (matches.Count == 0){
                        Console.WriteLine("setting version to " + version);
                        UmbracoVersion = version;
                        RunTarget("NugetPush");
                    }
                    else{
                        Console.WriteLine("Found #" + matches.Count + " in file build not needed");
                    }
                }

            }
        }
     }
    
});

Task("HardClean")
    .Does(() => 
    {
        var directoriesToClean = GetDirectories("./Umbraco-CMS/src/**/bin/");
        CleanDirectories(directoriesToClean);
        directoriesToClean = GetDirectories("./Umbraco-CMS/src/**/obj/");
        CleanDirectories(directoriesToClean);
    });

Task("CheckoutTag")
    .IsDependentOn("CloneUmbraco")
    .Does(()=>{
    Console.WriteLine("tags: ");
    var gitTags = GitTags(UmbracoFolder); // git tag --list 'release-7*'
    foreach (var tag in gitTags)
    {
        var tagName = tag.ToString();
        if (tagName.Contains("release-") && !tagName.Contains("release-netcore-"))
        {
            Console.WriteLine("tagName: " + tagName);
            var version = tagName.Substring(tagName.IndexOf("-") + 1 );
            var firstDot = version.IndexOf(".");
            var secondDot = version.IndexOf(".", firstDot + 1);
            var minor = "0";
            if (secondDot > 0){
                minor = version.Substring(firstDot + 1, secondDot - firstDot -1);
            }
            var major = version.Substring(0, firstDot);
            if (int.Parse(major) >= 7 && int.Parse(minor) >= 10){
                Console.WriteLine("version: " + version + " major: " + major + " minor " + minor);
            }
            if (int.Parse(major) == 8){
                Console.WriteLine("version: " + version + " major: " + major + " minor " + minor);
            }
        }
    }
    var exitCodeWithArgument = StartProcess("git", new ProcessSettings{ Arguments = "checkout release-" + UmbracoVersion, WorkingDirectory = UmbracoFolder });
});

Task("BuildTest")
    .IsDependentOn("CheckoutTag")
    .IsDependentOn("NugetRestoreUmbraco")
    .Does(()=> {
        var nugetDir = MakeAbsolute(Directory(UmbracoFolder + "/src/packages/"));
        Console.WriteLine("NugetDir: " + nugetDir.FullPath);
         MSBuild(UmbracoTestProj, new MSBuildSettings(){
             ToolVersion = MSBuildToolVersion.VS2022,
             Verbosity = Verbosity.Quiet
         }
         .WithProperty("NugetPackages", nugetDir.FullPath)
         .WithProperty("LangVersion", "9.0") // interfaces does not accept this
        //  .WithProperty("LangVersion", "7.3") // interfaces does not accept this
        //  .WithProperty("LangVersion", "6")
        // .WithProperty("LangVersion", "Default")
         );
});

Task("NugetPack")
    .IsDependentOn("BuildTest")
    .Does(()=>{
        var nuspecFile = "./Our.Umbraco.Community.Tests/Package.nuspec";
        Console.WriteLine("Nuget pack UmbracoVersion: " + UmbracoVersion + " postfix: " + postfix);
        var convertedVersion = SemVersion.TryParse (UmbracoVersion, out var version);
        Console.WriteLine("Nuget pack version: " + version + " postfix: " + postfix);

        NuGetPack(nuspecFile, new NuGetPackSettings{
                Version = version + postfix,
                OutputDirectory = "./Our.Umbraco.Community.Tests/"
            }
        );

});

Task("NugetPush")
    .IsDependentOn("NugetPack")
    .Does(()=>{
        var nugetPackage = "./Our.Umbraco.Community.Tests/Our.Umbraco.Community.Tests." + UmbracoVersion + postfix + ".nupkg";
        if (UmbracoVersion.Count(f => f == '.') == 1){
            // e.g. version 8.8 should be 8.8.0
            nugetPackage = "./Our.Umbraco.Community.Tests/Our.Umbraco.Community.Tests." + UmbracoVersion + ".0" + postfix + ".nupkg";
        }

        // get api from environment
        var apiKey = EnvironmentVariable("OurUmbracoCommunityTestNugetApiKey");
        if (string.IsNullOrEmpty(apiKey)){
            apiKey = Environment.GetEnvironmentVariable("OurUmbracoCommunityTestNugetApiKey", EnvironmentVariableTarget.User);
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("The NUGET_APIKEY environment variable is not defined.");
        }

        var packageId = "Our.Umbraco.Community.Tests";

        var isNuGetPublished = IsNuGetPublished(packageId, UmbracoVersion, "https://api.nuget.org/v3/index.json" );
        if (isNuGetPublished){
            Console.WriteLine("Nuget is already published " + UmbracoVersion + " " + isNuGetPublished);
            RunTarget("AppendUmbracoVersionToVersionFile");
            // throw new Exception("already published " + packageId + " version " + UmbracoVersion);
        } else{
            Console.WriteLine("Nuget is not published " + UmbracoVersion + " " + isNuGetPublished);
            NuGetPush(nugetPackage, new NuGetPushSettings {
                Source = "https://nuget.org/",
                ApiKey = apiKey
            });
            RunTarget("AppendUmbracoVersionToVersionFile");
        }
        RunTarget("GitPush");
});


Task("AppendUmbracoVersionToVersionFile")
.Does(()=>{
        var versionFile = "versions.txt";
        FileAppendLines(versionFile, new [] { UmbracoVersion });
});

Task("GitPush")
    .Does(()=> {
        Console.WriteLine("Git add all");
        GitAddAll(".");
        Console.WriteLine("git commit");
        GitCommit(".", "kedde", "kedde@kedde.dk", "add version " + UmbracoVersion);
        Console.WriteLine("git push");
        var exitPush = StartProcess("git", new ProcessSettings{ Arguments = "push" });
});

Task("FindVersion")
.Does(()=>{
    var versionFile = "versions.txt";
    var matches = FindRegexMatchesInFile(versionFile, UmbracoVersion, System.Text.RegularExpressions.RegexOptions.None);
    if (matches.Count > 0){
        Console.WriteLine("Found #" + matches.Count + " in file ");
    }
});

Task("NugetRestoreUmbraco").Does(()=>{
    NuGetRestore(UmbracoSolution);
});

Task("CopyUmbracoDllAndPdbs").Does(()=>{
    var binFolder = UmbracoFolder + "/src/Umbraco.Web.UI/bin/"; // UmbracoCms\src\Umbraco.Web.UI\bin\
    // copy to bin
    var photoSystemFolder = "../../src/Photo.Web/bin/";
    CopyFiles(binFolder + "umbraco*.dll", photoSystemFolder);
    CopyFiles(binFolder + "umbraco*.pdb", photoSystemFolder);

    // copy to nuget package folder
    var packageFolder = "../../packages/UmbracoCms.Core." + UmbracoVersion  +"/lib/net45/";
    Console.WriteLine("package folder" + packageFolder);
    CopyFiles(binFolder + "umbraco*.dll", packageFolder);
    CopyFiles(binFolder + "umbraco*.pdb", packageFolder);
});

Task("BuildUmbraco").Does(()=>{
        MSBuild(UmbracoSolution);
});

Task("CloneAndBuildUmbraco")
    .IsDependentOn("CloneUmbraco")
    .Does(() => {
        RunTarget("NugetRestoreUmbraco");
        RunTarget("BuildUmbraco");
        RunTarget("CopyUmbracoDllAndPdbs");
});


RunTarget(target);








