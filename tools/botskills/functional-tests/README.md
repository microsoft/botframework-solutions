# Functional Tests for Botskills
Follow these [steps](https://microsoft.github.io/botframework-solutions/solution-accelerators/tutorials/enable-continuous-integration/typescript/3-configure-build-steps/) to configure the functional tests using the `Nightly-botskills.yml`.

## Prerequisite
- Sign up for Azure DevOps
- Log in your [Azure DevOps’](https://dev.azure.com/) organization
- Have a YAML file in a repository to generate the build pipeline

## Variables

| Type | Variable | Description |
|------|----------|-------------|
| Azure Variable | system.debug | System variable that can be set by the user. Set this to true to run the release in [debug](https://docs.microsoft.com/en-us/azure/devops/pipelines/release/variables?view=azure-devops&tabs=batch#debug-mode) mode to assist in fault-finding |
|      | BuildConfiguration | Build configuration such as Debug or Release |
|      | BuildPlatform | Build platform such as Win32, x86, x64 or any cpu |
| Bot Variable | VirtualAssistantBotAppId | Microsoft App Id of the Virtual Assistant bot |
|      | VirtualAssistantBotAppPassword | Microsoft App Password of the Virtual Assistant bot |
|      | VirtualAssistantBotName | Name of the Virtual Assistant bot |
|      | SkillBotAppId | Microsoft App Id of the Skill bot |
|      | SkillBotAppPassword | Microsoft App Password of the Skill bot |
|      | SkillBotName | Name of the Skill bot |
|      | endpoints.0.endpointUrl | Skill Manifest endpoint url |
|      | endpoints.0.msAppId | Skill Manifest Microsoft App Id |
|      | privacyUrl | Skill Manifest privacy url |
|      | Location | Location where the bots will be published |
|      | LuisAuthoringRegion | Location of the LUIS apps |
|      | ServicePrincipal | App Id of the Service Principal |
|      | Azure_Tenant | Tenant's value of your Azure directory |
|      | AzureDevOps-ServicePrincipal-Secret | Secret of the Service Principal |

Last but not least, as the `Azure Subscription` is related to the container where the resources are created, it should be replaced with your Agent pool.

> system.debug, BuildPlatform and BuildConfiguration variables should be configured checking the Let users override this value when running this pipeline_ option.

## Steps contained in the YAML
1. Prepare: Use Node 10.16.3
1. Prepare: Use NuGet 4.9.1
1. Prepare: Delete preexisting resources
1. Prepare: Install preview dispatch
1. Prepare: Install preview botframework-cli
1. Prepare: Get CLI versions
1. Prepare: Replace Skill manifest properties
1. Prepare: VA - Restore dependencies
1. Prepare: VA - Build project
1. Prepare: Skill - Restore dependencies
1. Prepare: Skill - Build project
1. Prepare: VA - Deploy
1. Prepare: VA - Get variables from appsettings
1. Prepare: Skill - Deploy
1. Prepare: Skill - Get variables from appsettings
1. Build: Botskills - Install dependencies
1. Build: Botskills - Build project
1. Link: Botskills
1. Build: Botskills - Execute unit tests
1. Debug: Botskills - Publish test results
1. Debug: Botskills - Publish test coverage
1. Test: Botskills - Execute connect command without refresh
1. Test: Botskills - Execute list command
1. Test: Botskills - Execute update command without refresh
1. Debug: Botskills - Get connected Skills to the VA
1. Test: Botskills - Execute refresh command
1. Test: Botskills - Execute disconnect command without refresh
1. Test: Botskills - Execute list command
1. Debug: dir workspace

## Further Reading
- [What is Azure Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/get-started/what-is-azure-pipelines?view=azure-devops)
- [Define variables - Azure Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/process/variables?view=azure-devops&tabs=yaml%2Cbatch)