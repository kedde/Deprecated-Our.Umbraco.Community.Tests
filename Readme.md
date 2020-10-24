# Our.Umbraco.Community.Tests

## building the Out.Umbraco.Tests

Run the following command to build the Umbraco test project

``` powershell
.\build.ps1 -Target CheckRemoteTagsAndBuildIfNeeded
```

This project is using the cake build system - <https://cakebuild.net/> and build targets are found in build.cake.

## setup 

Some steps I had to do when trying to get the test to work with a Umbraco7 site: 

Put minimal configuration file in the test project:

cd [TestProject]\Configurations\UmbracoSettings
wget https://raw.githubusercontent.com/umbraco/Umbraco-CMS/main-v7/src/Umbraco.Tests/Configurations/UmbracoSettings/umbracoSettings.minimal.config  -OutFile umbracoSettings.minimal.config

Include the file in the solution and right click select properties and choose "Copy if newer" in "Copy to output directory"-property.

running umbraco 7: 

* I needed to use nunit 2.6.2
* installed .net framework 3.5 to get errors when using nunit-console to see extended errors? 
* .\packages\NUnit.Runners.2.6.2\tools\nunit-console.exe .\test\[TestProject]\bin\Debug\[TestProject].Tests.dll


## Continuous Integration - app veyor

A daily cron job is run, where it checks for new versions of Umbraco-CMS see status here:

https://ci.appveyor.com/project/kedde/umbracobuild/

## references for unit test

* <http://blog.aabech.no/archive/the-basics-of-unit-testing-umbraco-just-got-simpler/>
* <https://skrift.io/articles/archive/unit-testing-umbraco-with-umbraco-context-mock/>
* <https://github.com/umbraco/Umbraco-CMS/tree/v8/dev/src/Umbraco.Tests>
