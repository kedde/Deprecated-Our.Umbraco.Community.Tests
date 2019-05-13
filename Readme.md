# Our.Umbraco.Community.Tests

## building the Out.Umbraco.Tests

Run the following command to build the Umbraco test project

``` powershell
.\build.ps1 -Target CheckRemoteTagsAndBuildIfNeeded
```

This project is using the cake build system - <https://cakebuild.net/> and build targets are found in build.cake.

## Continuous Integration - app veyor

A daily cron job is run, where it checks for new versions of Umbraco-CMS see status here:

https://ci.appveyor.com/project/kedde/umbracobuild/

## references for unit test

* <http://blog.aabech.no/archive/the-basics-of-unit-testing-umbraco-just-got-simpler/>
* <https://skrift.io/articles/archive/unit-testing-umbraco-with-umbraco-context-mock/>
* <https://github.com/umbraco/Umbraco-CMS/tree/v8/dev/src/Umbraco.Tests>