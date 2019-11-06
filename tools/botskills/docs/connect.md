# Connect a Skill to your Virtual Assistant

The `connect` command allows you to connect a Skill, be it local or remote, to your Virtual Assistant bot. The Skill and Virtual Assistant can be in different coding languages without problem, this is, you can connect a Skill coded in C# into an Virtual Assistant coded in TypeScript, but be sure to specify your Virtual Assistants coding language using `--cs` or `--ts`.

> **Tip:** It's highly advisable to execute this command from the **root folder of your Virtual Assistant bot**, so if you are using the suggested folder structure from the Templates, you may omit most of the optional arguments, as they default to the expected values from the Template's folder structure.

The basic command to connect a Skill to your Virtual Assistant:

```bash
botskills connect [options]
```

## Skill Deployment
See the [Skills Overview]({{site.baseurl}}/overview/skills) section for details on the Skills provided as part of the Virtual Assistant Solution Accelerator. Follow the deployment instructions required for each skill you wish to use and then return to this section to add these skills to your Virtual Assistant.

## Manual Authentication Connection configuration

If a Skill requires Authentication connections to Office/Office 365 in most cases the above script will automatically add this configuration to your Bot and associated Azure AD Application.

In the case that your Azure AD application has allowed users outside of your tenant to access the Application this auto-provisioning isn't possible and the CLI may warn that it wasn't able to configure Scopes and provides the Scopes you should manually add. An example of this is shown below:

```
Could not configure scopes automatically. You must configure the following scopes in the Azure Portal to use this skill: User.Read, User.ReadBasic.All, Calendars.ReadWrite, People.Read
```

In this situation for Microsoft Graph based skills follow the instructions below:

1. Find the Azure AD Application for your Bot within the [Azure Portal](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredAppsPreview)
2. In the Authentication section ensure the Redirect Uri is set to `https://token.botframework.com/.auth/web/redirect` and Supported Account Types is set to `Accounts in any organizational directory`.
3. In the API permissions section click Add Permission, then Microsoft Graph and Delegated Permissions. Find each scope provided in the message shown during Skill registration and add.

For Skills that require other Authentication connection configuration please follow the skill specific configuration information.

### Options

| Option                        | Description                                                                                                                                                                         |
|-------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| -l, --localManifest \<path>   | Path to local Skill Manifest file                                                                                                                                                   |
| -r, --remoteManifest \<url>   | URL to remote Skill Manifest                                                                                                                                                        |
| --cs                          | Determine your Virtual Assistant project structure to be a csharp-like structure                                                                                                    |
| --ts                          | Determine your Virtual Assistant project structure to be a TypeScript-like structure                                                                                                |
| --noRefresh                   | (OPTIONAL) Determine whether the model of your skills connected are not going to be refreshed (by default they are refreshed)                                                       |
| --dispatchName [name]         | (OPTIONAL) Name of your Virtual Assistant's `.dispatch` file (defaults to the name displayed in your Cognitive Models file)                                                         |
| --language [language]         | (OPTIONAL) Locale used for LUIS culture (defaults to `en-us`)                                                                                                                       |
| --luisFolder [path]           | (OPTIONAL) Path to the folder containing your Skills' `.lu` files (defaults to `./deployment/resources/skills/en` inside your Virtual Assistant folder)                             |
| --dispatchFolder [path]       | (OPTIONAL) Path to the folder containing your Virtual Assistant's `.dispatch` file (defaults to `./deployment/resources/dispatch/en` inside your Virtual Assistant folder)          |
| --outFolder [path]            | (OPTIONAL) Path for any output file that may be generated (defaults to your Virtual Assistant's root folder)                                                                        |
| --lgOutFolder [path]          | (OPTIONAL) Path for the LuisGen output (defaults to a 'service' folder inside your Virtual Assistant's folder)                                                                      |
| --skillsFile [path]           | (OPTIONAL) Path to your Virtual Assistant Skills configuration file (defaults to the `skills.json` inside your Virtual Assistant's folder)                                           |
| --resourceGroup [name]        | (OPTIONAL) Name of your Virtual Assistant's resource group in Azure (defaults to your Virtual Assistant's bot name)                                                                 |
| --appSettingsFile [path]      | (OPTIONAL) Path to your appsettings file (defaults to `appsettings.json` inside your Virtual Assistant's folder)                                                                    |
| --cognitiveModelsFile [path]  | (OPTIONAL) Path to your Cognitive Models file (defaults to `cognitivemodels.json` inside your Virtual Assistant's folder)                                                           |
| --verbose                     | (OPTIONAL) Output detailed information about the processing of the tool                                                                                                             |
| -h, --help                    | Output usage information                                                                                                                                                            |

An example on how to use it with a local Skill manifest file:

```bash
botskills connect --localManifest "./skills/customSkill/customSkillManifest.json" --skillsFile "./skills.json" --cs --verbose
```

> **Note:** The paths to both the Skill Manifest and the Virtual Assistant Skills configuration file can be relative or absolute paths equally, and should be explicitly a `.json` file.

An example on how to use it with a remote Skill manifest:

```bash
botskills connect --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest?inlineTriggerUtterances=false" --skillsFile "./skills.json" --cs --verbose
```