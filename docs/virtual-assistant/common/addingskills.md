# ![Conversational AI Solutions](/docs/media/conversationalai_solutions_header.png)

## Pre-requisites
- TBC Skill CLI

## Skill Deployment

See the [Skills Overview](/docs/skills/csharp/README.md) section for details on the Skills provided as part of the Virtual Assistant Solution Accelerator. Follow the deployment instructions required for each skill you wish to use and then return to this section to add these skills to your Virtual Assistant.

## Adding Skills to your Virtual Assistant

1. In **PowerShell Core** (pwsh.exe), change to the project directory for your Virtual Assistant
2. Run the following command passing the name of your Bot and a pointer to the manifest endpoint of your Skill.
    ```
    .\Deployment\scripts\add_remote_skill.ps1 -botName "YOUR_BOT_NAME" -manifestUrl https://YOUR_SKILL.azurewebsites.net/api/skill/manifest
    ```

If a Skill requires Authentication connections to Office/Office 365 in most cases the above script will automatically add this configuration to your Bot and associated Azure AD Application. 

In the case that your Azure AD application has allowed users outside of your tenant to access the Application this auto-provisioning isn't possible and the above script may warn that it wasn't able to configure Scopes and provides the Scopes you should manually add. An example of this is shown below:
```
Could not configure scopes automatically. You must configure the following scopes in the Azure Portal to use this skill: User.Read, User.ReadBasic.All, Calendars.ReadWrite, People.Read
```

In this situation for Microsoft Graph based skills follow the instructions below:

1. Find the Azure AD Application for your Bot within the [Azure Portal](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredAppsPreview)
2. In the Authentication section ensure the Redirect Uri is set to `https://token.botframework.com/.auth/web/redirect`
3. In the API permissions section click Add Permission, then Microsoft Graph and Delegated Permissions. Find each scope provided in the message shown during Skill registration and add.

For Skills that require other Authentication connection configuration please follow the skill specific configuration information.


## Updating an existing Skill to reflect changes to Actions or LUIS model

TBC

## Removing a Skill from your Virtual Assistant

TBC

