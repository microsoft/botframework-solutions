# Functional Tests for C# Virtual Assistant
Follow these [steps](https://microsoft.github.io/botframework-solutions/solution-accelerators/tutorials/enable-continuous-integration/csharp/4-configure-build-steps/) to configure the functional tests using the `Nightly-Dotnet-VirtualAssistantToSkill.yml`.

Currently, adding this YAML in your Azure DevOps organization enables you to **validate** the following scenarios using the last preview version of the packages from the daily builds:
- Use of [dispatch](https://botbuilder.myget.org/feed/botbuilder-tools-daily/package/npm/botdispatch), [luis-apis](https://botbuilder.myget.org/feed/botbuilder-tools-daily/package/npm/luis-apis) and [botskills](https://botbuilder.myget.org/feed/aitemplates/package/npm/botskills)
- Use of [@microsoft/botframework-cli](https://botbuilder.myget.org/feed/botframework-cli/package/npm/@microsoft/botframework-cli)
- Use of [SDK](https://botbuilder.myget.org/gallery/botbuilder-v4-dotnet-daily) incorporated in the C# Virtual Assistant
- Deployment of the C# Virtual Assistant and C# Skill
- Communication with the C# Virtual Assistant and C# Skill
- Connect Virtual Assistant with a Skill, both in C#

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
|      | iconUrl | Icon Uri representing your skill, potentially used to show the skills registered with a Bot |
|      | Location | Location where the bots will be published |
|      | privacyUrl | Skill Manifest privacy url |
|      | LuisAuthoringKey | XXXX |
|      | LuisAuthoringRegion | Location of the LUIS apps |
|      | BotBuilderPackageVersion | Version of the SDK's packages that the bot will use |
|      | ServicePrincipal | App Id of the Service Principal |
|      | Azure_Tenant | Tenant's value of your Azure directory |
|      | AzureDevOps-ServicePrincipal-Secret | Secret of the Service Principal |

Last but not least, as the `Azure Subscription` is related to the container where the resources are created, it should be replaced with your Agent pool.

> **Note**: system.debug, BuildPlatform and BuildConfiguration variables should be configured checking the "Let users override this value when running this pipeline" option.

## Steps contained in the YAML
1. Prepare: Use Node 10.16.3
1. Prepare: Use NuGet 4.9.1
1. Prepare: Delete preexisting resources
1. Prepare: Install preview dispatch
1. Prepare: Install preview botframework-cli
1. Prepare: Install preview botskills
1. Prepare: Get CLI and SDK versions
1. Prepare: Replace BotBuilder version in .csproj files
1. Prepare: Replace Skill manifest properties
1. Build: VA - Restore dependencies
1. Build: VA - Build project
1. Build: VA - Execute unit tests
1. Build: Skill - Restore dependencies
1. Build: Skill - Build project
1. Build: Skill - Execute unit tests
1. Deploy: VA
1. Deploy: VA - Get variables from appsettings
1. Deploy: Skill
1. Deploy: Skill - Get variables from appsettings
1. Test: Skill - Create Direct Line registration
1. Test: Skill - Get channel secrets
1. Test: Skill - Execute functional tests
1. Test: VA - Connect SkillSample
1. Build: VA - Build project
1. Test: VA - Publish with connected Skill
1. Test: VA - Create Direct Line registration
1. Test: VA - Get channel secrets
1. Test: VA - Execute functional tests
1. Cleanup: Delete resources (disabled)
1. Debug: VA - Show log contents
1. Debug: Skill - Show log contents
1. Debug: dir workspace

## Further Reading
- [What is Azure Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/get-started/what-is-azure-pipelines?view=azure-devops)
- [Define variables - Azure Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/process/variables?view=azure-devops&tabs=yaml%2Cbatch)