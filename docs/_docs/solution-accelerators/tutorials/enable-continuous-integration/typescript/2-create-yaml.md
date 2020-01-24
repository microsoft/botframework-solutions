---
layout: tutorial
category: Solution Accelerators
subcategory: Enable continuous integration
title: Create a YAML file
language: Typescript
order: 2
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
    - 'templates/typescript/samples/sample-assistant/*'

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
    workingDir: 'templates/typescript/samples/sample-assistant'
    verbose: false

- task: Npm@1
  displayName: 'npm run build'
  inputs:
    command: custom
    workingDir: 'templates/typescript/samples/sample-assistant'
    verbose: false
    customCommand: 'run build'

- task: Npm@1
  displayName: 'npm test - coverage'
  inputs:
    command: custom
    workingDir: 'templates/typescript/samples/sample-assistant'
    verbose: false
    customCommand: 'run coverage'

- task: PublishTestResults@2
  displayName: 'publish test results'
  inputs:
    testResultsFiles: 'test-results.xml'
    searchFolder: 'templates/typescript/samples/sample-assistant'
    failTaskOnFailedTests: true

- task: PublishCodeCoverageResults@1
  displayName: 'publish code coverage'
  inputs:
    codeCoverageTool: Cobertura
    summaryFileLocation: 'templates/typescript/samples/sample-assistant/coverage/cobertura-coverage.xml'
    reportDirectory: 'templates/typescript/samples/sample-assistant/coverage/'
```
By default the build pipelines automatically triggers a build on each new pull request. This can be changed to run against the master branch with the following change:

```diff
- pr: none
+ pr: 
+ - master
```