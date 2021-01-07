# Skill: Validating Skill functionality

## Summary

Functional tests aim to ensure to ensure Skills function correctly across the breadth of the Bot Framework.

### High level design goals

1. Validate existing functionality consistently, identify issues and potential regressions.
2. New functionality can be easily tested, without the need to recreate the complex topologies required when working with Virtual Assistant and Skills.
3. The test infrastructure can be used either directly or as a template for support scenarios to repro customer issues.
4. Execute automated functional tests regularly (as part of the CI/CD pipeline, on a regular schedule or triggered manually).
5. Ensure a Virtual Assistant and a Skill built with any of the languages supported by the SDK will work with any other bot built with a different language SDK.

To support these goals, the testing infrastructure used to validate the functional tests derived from this document must be carefully considered.

## Contents

- [Scenarios](#scenarios)
    - [1. Skill greets a new user](#1-skill-greets-a-new-user)
- [Reference](#reference)
    - [Variables](#variables)
- [Glossary](#glossary)

## Scenarios

This section describes the testing scenarios for Skills, for each one of them we provide a high level goal description of the primary test case.

The variables section lists the set of [variables](#variables) that apply to the test cases and need to be configured.

### 1. Skill greets a new user

> A user logs in the conversation and the Skill sends the Welcome message.

## Reference

### Variables
- AppId: Microsoft App Id of the bot
- AppPassword: Microsoft App Password of the bot
- AzureDevOps-ServicePrincipal-Secret: Secret of the Service Principal
- AzureSubscription: the name of your Azure Subscription
- Azure_Tenant: Tenant's value of your Azure directory
- BotBuilderPackageVersion: version of the BotBuilder package
- BotLanguages: the supported languages of your bot
- BotName: name of the bot
- BuildConfiguration: build configuration such as Debug or Release
- BuildPlatform: build platform such as Win32, x86, x64 or any cpu
- Location: location of the bot
- LuisAuthoringRegion: location of the LUIS apps
- ServicePrincipal: App Id of the Service Principal
- system.debug: system variable that can be set by the user. Set this to true to run the release in [debug](https://docs.microsoft.com/en-us/azure/devops/pipelines/release/variables?view=azure-devops&tabs=batch#debug-mode) mode to assist in fault-finding

Last but not least, as the `Azure Subscription` is related to the container where the resources are created, it should be replaced with your Agent pool.

> BuildConfiguration, BuildPlatform and system.debug variables should be configured checking the "Let users override this value when running this pipeline" option.

## Glossary
- **Virtual Assistant:** A bot that passes Activities to another bot (a Skill) for processing.
- **Skill:** A bot that accepts Activities from another bot (a Virtual Assistant), and passes Activities to users through that Virtual Assistant.