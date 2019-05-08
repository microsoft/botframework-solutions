# Adding Skills

## Pre-requisites

- [Node.js](https://nodejs.org/) version 10.8 or higher
- Dispatch CLI which is used as part of skill registration

    ```shell
    npm install -g botdispatch
    ```

## Adding Skills

We have two approaches to add Skills to your Virtual Assistant which can be used interchangeably.

1. The first leverages a `add_remote_skill.ps1` PowerShell script included as part of your Virtual Assistant solution (located in the `Deployment\Scripts` folder of your assistant).
2. `botskills` command line tool which is in preview.

> Skills requiring authentication (e.g. the productivity skills) should be added using the PowerShell script at this time.

If you wish to use the `botskills` CLI then you can install using the following npm command:

```bash
npm install -g botdispatch, botskills
```

> Your Virtual Assistant must have been deployed using the [deployment tutorial](/docs/tutorials/assistantandskilldeploymentsteps.md) before using the add_remote_skill `botskills` tool as it relies on the Dispatch models being available and a deployed Bot for authentication connection information.

## Skill Deployment

See the [Skills Overview](/docs/README.md#skills) section for details on the Skills provided as part of the Virtual Assistant Solution Accelerator. Follow the deployment instructions required for each skill you wish to use and then return to this section to add these skills to your Virtual Assistant.

## Skill CLI

The PowerShell script and CLI provides automation of all key steps required to add a Skill to your project

1. Retrieve the Skill Manifest from the remote Skill through the `/api/skill/manifest` endpoint.
2. Identify which Language Models are used by the Skill and resolve the triggering utterances either through local LU file resolution or through inline trigger utterances if requested.
3. Add a new dispatch target using the `dispatch` tool using the trigger utterances retrieved in the previous step.
4. Refresh the dispatch LUIS model with the new utterances
5. In the case of Active Directory Authentication Providers, an authentication connection will be added to your Bot automatically and the associated Scopes added to your Azure AD application that backs your deployed Assistant.

## Adding Skills to your Virtual Assistant

Run the following command to add each Skill to your Virtual Assistant. This assumes you are running the CLI within the project directory and have created your Bot through the template and therefore have a `skills.json` file present.

The `--luisFolder` parameter can be used to point the Skill CLI at the source LU files for trigger utterances. For Skills provided within this repo these can be found in the `Deployment\Resources\LU` folder of each Skill. The CLI will automatically traverse locale folder hierarchies.  This can be omitted for any of the skills we provide as the LU files are provided locally.

- PowerShell:

```powershell
.\Deployment\scripts\add_remote_skill.ps1 -botName "YOUR_BOT_NAME" -manifestUrl https://YOUR_SKILL.azurewebsites.net/api/skill/manifest -luisFolder [path]
```

- botskills CLI:

```bash
botskills connect --botName YOUR_BOT_NAME --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --luisFolder [path] --cs
```

See the [Skill CLI documentation](/lib/typescript/botskills/docs/connect-disconnect.md) for detailed CLI documentation,

## Manual Authentication Connection configuration

If a Skill requires Authentication connections to Office/Office 365 in most cases the above script will automatically add this configuration to your Bot and associated Azure AD Application.

In the case that your Azure AD application has allowed users outside of your tenant to access the Application this auto-provisioning isn't possible and the CLI may warn that it wasn't able to configure Scopes and provides the Scopes you should manually add. An example of this is shown below:

```
Could not configure scopes automatically. You must configure the following scopes in the Azure Portal to use this skill: User.Read, User.ReadBasic.All, Calendars.ReadWrite, People.Read
```

In this situation for Microsoft Graph based skills follow the instructions below:

1. Find the Azure AD Application for your Bot within the [Azure Portal](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredAppsPreview)
2. In the Authentication section ensure the Redirect Uri is set to `https://token.botframework.com/.auth/web/redirect`
3. In the API permissions section click Add Permission, then Microsoft Graph and Delegated Permissions. Find each scope provided in the message shown during Skill registration and add.

For Skills that require other Authentication connection configuration please follow the skill specific configuration information.

## Removing a Skill from your Virtual Assistant

To disconnect a skill from your Virtual Assistant use the following command, passing the id of the Skill as per the manifest (e.g. calendarSkill).

- PowerShell:

```powershell
.\Deployment\scripts\remove_skill.ps1 -manifestUrl https://YOUR_SKILL.azurewebsites.net/api/skill/manifest
```

botskills:

```bash
botskills disconnect --skillId SKILL_ID
```

## Updating an existing Skill to reflect changes to Actions or LUIS model

> A botskills refresh command will be added shortly. In the meantime, run the above disconnect command and then connect the skill again.