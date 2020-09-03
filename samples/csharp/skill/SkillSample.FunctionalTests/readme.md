# Functional Tests for C# Skill
Follow these [steps](https://microsoft.github.io/botframework-solutions/solution-accelerators/tutorials/enable-continuous-integration/csharp/4-configure-build-steps/) to configure the functional tests using the `SkillSample.yml`.

Currently, adding this YAML in your Azure DevOps organization enables you to **validate** the following scenarios using the last preview version of the packages from the daily builds:
- Use of [dispatch](https://botbuilder.myget.org/feed/botbuilder-tools-daily/package/npm/botdispatch), [luis-apis](https://botbuilder.myget.org/feed/botbuilder-tools-daily/package/npm/luis-apis) and [botskills](https://botbuilder.myget.org/feed/aitemplates/package/npm/botskills)
- Use of [@microsoft/botframework-cli](https://botbuilder.myget.org/feed/botframework-cli/package/npm/@microsoft/botframework-cli)
- Use of [SDK](https://botbuilder.myget.org/gallery/botbuilder-v4-dotnet-daily) incorporated in the C# Skill
- Deployment of the C# Skill
- Communication with the C# Skill

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
| Bot Variable | AppId | Microsoft App Id of the bot |
|      | AppPassword | Microsoft App Password of the bot |
|      | BotName | Name of the bot |
|      | Location | Location of the bot |
|      | LuisAuthoringRegion | Location of the LUIS apps |
|      | BotBuilderPackageVersion | Version of the SDK's packages that the bot will use |
|      | ServicePrincipal | App Id of the Service Principal |
|      | Azure_Tenant | Tenant's value of your Azure directory |
|      | AzureDevOps-ServicePrincipal-Secret | Secret of the Service Principal |

Last but not least, as the `Azure Subscription` is related to the container where the resources are created, it should be replaced with your Agent pool.

## Steps contained in the YAML
1. Prepare: Use Node 10.16.3
1. Prepare: Use NuGet 4.9.1
1. Prepare: Delete test resource group if it exists
1. Prepare: Install preview dispatch, luis-apis, botskills
1. Prepare: Install preview botframework-cli
1. Prepare: Get CLI and SDK versions
1. Prepare: Replace BotBuilder version in .csproj file
1. Build: Restore dependencies
1. Build: Build project
1. Build: Execute unit tests
1. Deploy: Skill
1. Deploy: Get bot variables from appsettings
1. Deploy: Create Direct Line channel registration
1. Deploy: Get channel secrets
1. Test: Run dotnet test on functional tests
1. Cleanup: Delete bot resources
1. Debug: Show log contents
1. Debug: Dir workspace

## Further Reading
- [What is Azure Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/get-started/what-is-azure-pipelines?view=azure-devops)
- [Define variables - Azure Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/process/variables?view=azure-devops&tabs=yaml%2Cbatch)