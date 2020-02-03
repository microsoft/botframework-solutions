---
layout: tutorial
category: Solution Accelerators
subcategory: Enable continuous integration
title: Create a YAML file
language: csharp
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}
{:.no_toc}

Create a **YAML** file with the configured that your build pipeline will use. This will be custom to your requirements.

The sample here shows a **YAML** file configured to the [Bot Framework Solutions repository]({{site.repo}}). It can be used in any location in the repository without affecting the file's functionality.

```yaml
# specific branch build
trigger:
  branches:  
    include:
    - master
    - feature/*

  paths:
    include:
    - 'templates/csharp/VA/*'

# By default will disable PR builds
pr: none

pool:
   name: Hosted VS2017
   demands:
    - msbuild
    - visualstudio

variables:
  buildPlatform: 'Any CPU'
  buildConfiguration: 'Release'

steps:
- task: DotNetCoreInstaller@0
  displayName: 'Use .NET Core sdk 2.2.100'
  inputs:
    version: 2.2.100
  continueOnError: true

- task: NuGetToolInstaller@0
  displayName: 'Use NuGet 4.9.1'
  inputs:
    versionSpec: 4.9.1

- task: NuGetCommand@2
  displayName: 'NuGet restore'
  inputs:
    restoreSolution: 'templates\csharp\Templates.sln'

- task: VSBuild@1
  displayName: 'Build solution VirtualAssistantTemplate.sln'
  inputs:
    solution: templates\csharp\Templates.sln
    vsVersion: '16.0'
    platform: '$(buildPlatform)'
    configuration: '$(buildConfiguration)'

- task: DotNetCoreCLI@2
  displayName: 'test results'
  inputs:
    command: test
    projects: '$(System.DefaultWorkingDirectory)\templates\csharp\VA\VA.Tests\VA.Tests.csproj'
    arguments: '-v n --configuration $(buildConfiguration) --no-build --no-restore --filter TestCategory!=IgnoreInAutomatedBuild /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura'
    workingDirectory: 'templates\csharp\VA\VA.Tests'

- task: PublishCodeCoverageResults@1
  displayName: 'Publish code coverage'
  inputs:
    codeCoverageTool: Cobertura
    summaryFileLocation: '$(Build.SourcesDirectory)\templates\csharp\VA\VA.Tests\coverage.cobertura.xml'
    reportDirectory: '$(Build.SourcesDirectory)\templates\csharp\VA\VA.Tests'
```
By default the build pipelines automatically triggers a build on each new pull request. This can be changed to run against the master branch with the following change:

```diff
- pr: none
+ pr: 
+ - master
```
