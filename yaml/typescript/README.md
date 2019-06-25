# Overview

Learn how to create a *Pipeline* using a `YAML` file as configuration, as it's an easy way to configure one or many specific branches with an existing `YAML` or creating a new one. You can add different scripts using the tools of *Pipelines* or writing directly the different Tasks that the *Pipeline* needs to execute.

## In this tutorial

- [Intro](#intro)
- [Create a YAML file](#create-a-YAML-file)
- [Configure build step by step in Pipelines](#Configure-build-step-by-step-in-Pipelines)

## Intro

### Prerequisites

Set up an *Azure DevOps* account. 

### Time to Complete

15 minutes

### Scenario

A personalized *Pipeline* in *Azure DevOps* usign a `YAML` file.

## Create a YAML file 

In first place, you need to create a `YAML` file with the configuration that the *Pipeline* will use. This is according to the needs of the user.

This is an example to configure the `YAML` file. You are able to create or add this file in the root of your project or in any location, this doesn't affect or change the functionality of the `YAML`.

```
# specific branch build
trigger:
  branches:  
    include:
    - master
    - feature/*

  paths:
    include:
    - 'templates/Virtual-Assistant-Template/typescript/samples/sample-assistant/*'

# By default will disable PR builds
pr: none

pool:
  name: Hosted VS2017
steps:
- task: NodeTool@0
  displayName: 'Use Node 10.x'
  inputs:
    versionSpec: 10.x

- task: Npm@1
  displayName: 'npm install'
  inputs:
    workingDir: 'templates/Virtual-Assistant-Template/typescript/samples/sample-assistant'
    verbose: false

- task: Npm@1
  displayName: 'npm run build'
  inputs:
    command: custom
    workingDir: 'templates/Virtual-Assistant-Template/typescript/samples/sample-assistant'
    verbose: false
    customCommand: 'run build'

- task: Npm@1
  displayName: 'npm test - coverage'
  inputs:
    command: custom
    workingDir: 'templates/Virtual-Assistant-Template/typescript/samples/sample-assistant'
    verbose: false
    customCommand: 'run coverage'

- task: PublishTestResults@2
  displayName: 'publish test results'
  inputs:
    testResultsFiles: 'test-results.xml'
    searchFolder: 'templates/Virtual-Assistant-Template/typescript/samples/sample-assistant'
    failTaskOnFailedTests: true

- task: PublishCodeCoverageResults@1
  displayName: 'publish code coverage'
  inputs:
    codeCoverageTool: Cobertura
    summaryFileLocation: 'templates/Virtual-Assistant-Template/typescript/samples/sample-assistant/coverage/cobertura-coverage.xml'
    reportDirectory: 'templates/Virtual-Assistant-Template/typescript/samples/sample-assistant/coverage/'
```

By default, the *Pipeline* automatically triggers a buid for each new pull-request. This *Pipeline's* behavior can be disabled using the following configuration:
```
pr: none
```
In case that you want to activate this, you can use the following configuration:
```
pr: 
- master
```

## Configure build step by step in Pipelines

1. With the `YAML` file configurated you can go to *Azure DevOps* site and proceed to add the new Pipeline. Selecting the *Pipelines* option, will appear the builds like the following screenshot: 

<p align="center">
<img src="../../docs/media/pipelines-build.png" width="500"/>
</p>

2. Then, selecting the option 'New', will add a new *Pipeline*. The next step will be to connect with the code and for that the recommended option is `GitHub with YAML`.

<p align="center">
<img src="../../docs/media/configure-new-pipeline.png"?raw=true width="900">
</p>

3. Select the repository that will include the builds

<p align="center">
<img src="../../docs/media/select-repository.png" width="700">
</p>

4. You will use an existing `YAML` file for this purpose.

<p align="center">
<img src="../../docs/media/configure-pipeline.png" width="600">
</p>

5. You can use the `YAML` file created before by completing the path with the location of the `YAML` file. It's also necessary to select the branch that has the file. 

<p align="center">
<img src="../../docs/media/branch-path.png" width="500">
</p>

6. The *Pipeline* was created successfully and you can see the configuration of the `YAML` file. The next step will be to run the `YAML` to start the build process.

<p align="center">
<img src="../../docs/media/run-build.png" width="900">
</p>