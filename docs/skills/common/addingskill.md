# Adding a Skill

## Pre-requisites
- TBC Skill CLI

## Skill Deployment

See the [Skills Overview](/docs/skills/README.md) section for details on the Skills provided as part of the Virtual Assistant Solution Accelerator. Follow the deployment instructions required for each skill you wish to use and then return to this section to add these skills to your Virtual Assistant.

## Adding Skills to your Virtual Assistant

1. In **PowerShell Core** (pwsh.exe), change to the project directory for your Virtual Assistant
2. Run the following command passing the name of your Bot, a pointer to the manifest endpoint of your Skill and the folder location where the corresponding LU files can be resolved.
    ```
    .\Deployment\scripts\add_remote_skill.ps1 -botName "YOUR_BOT_NAME" -manifestUrl https://YOUR_SKILL.azurewebsites.net/api/skill/manifest -luisFolder FOLDER_WITH_LU_FILES
    ```

    Parameter | Description | Required
    --------- | ----------- | --------
    botName | Name of your bot, required to add authentication connections as required by the Skill to your Bot| **Yes**
    manifestUrl | The URL of the Skill manifest endpoint (suffix of /api/skill/manifest) | **Yes**
    luisFolder | Location of the LU format files containining the triggering utterances for this Skill | **Yes**
    appSettingsFile | AppSettings configuration file name. Default value is `appsettings.config`| No
    skillsFile | Configured skills configuration file name. Default value is `skills.json` | No
    cognitiveModelsFile | Cognitive Models configuration file. Default value is `cognitivemodels.json` | No
    logFile | Log filename. Default value is `add_remote_skill_log.txt` | No

## Authentication Connections

If a Skill requires Authentication connections to Office/Office 365 (e.g. Calendar Skill) in most cases the above script will automatically add this configuration to your Bot and associated Azure AD Application. 

In the case that your Azure AD application has allowed users outside of your tenant to access the Application this auto-provisioning isn't possible and the above script may warn that it wasn't able to configure Scopes and provides the Scopes you should manually add. An example of this is shown below:
```
Could not configure scopes automatically. You must configure the following scopes in the Azure Portal to use this skill: User.Read, User.ReadBasic.All, Calendars.ReadWrite, People.Read
```

In this situation for Microsoft Graph based skills follow the instructions below:

1. Find the Azure AD Application for your Bot within the [Azure Portal](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredAppsPreview)
2. In the Authentication section ensure the Redirect Uri is set to `https://token.botframework.com/.auth/web/redirect`
3. In the API permissions section click Add Permission, then Microsoft Graph and Delegated Permissions. Find each scope provided in the message shown during Skill registration and add.

For Skills that require other Authentication connection configuration please follow the skill specific configuration information.