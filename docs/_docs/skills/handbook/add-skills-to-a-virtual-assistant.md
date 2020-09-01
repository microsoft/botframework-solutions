---
category: Skills
subcategory: Handbook
title: Add a Skill to a Virtual Assistant
description: Steps for adding a skill to an assistant
order: 6
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

## Prerequisites

- [Node.js](https://nodejs.org/) version 10.8 or higher
- Install the Dispatch and botframework-cli

    ```shell
    npm install -g botdispatch @microsoft/botframework-cli
    ```
- [.NET Core runtime](https://dotnet.microsoft.com/download/dotnet-core/2.1#runtime-2.1.0): ^2.1.0

## Adding Skills

To add your new Skill to your assistant/Bot we provide a [botskills](https://www.npmjs.com/package/botskills) command line tool to automate the process of adding the Skill to your dispatch model and creating authentication connections where needed. The CLI performs the following operations on your behalf:

1. Retrieve the Skill Manifest from the remote Skill through the `/manifest/manifest-1.1.json` endpoint.
1. Identify which Language Models are used by the Skill and resolve the triggering utterances either through local LU file resolution or through inline trigger utterances if requested.
1. Add a new dispatch target using the `dispatch` tool using the trigger utterances retrieved in the previous step.
1. Refresh the dispatch LUIS model with the new utterances
1. In the case of Active Directory Authentication Providers, an authentication connection will be added to your Bot automatically and the associated Scopes added to your Azure AD application that backs your deployed Assistant.

The `botskills` CLI can be installed using the following npm command:

```bash
npm install -g botskills@latest
```

> Your Virtual Assistant must have been deployed using the [deployment tutorial]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/4-provision-your-azure-resources) before using the `botskills` CLI as it relies on the Dispatch models being available and a deployed Bot for authentication connection information.

## Skill Deployment

See the [Skills Overview]({{site.baseurl}}/overview/skills) section for details on the Skills provided as part of the Virtual Assistant Solution Accelerator. Follow the deployment instructions required for each skill you wish to use and then return to this section to add these skills to your Virtual Assistant.

## Adding Skills to your Virtual Assistant

Run the following command to add each Skill to your Virtual Assistant. This assumes you are running the CLI within the project directory and have created your Bot through the template and therefore have a `appsettings.json` file present in the working folder.

```bash
botskills connect --remoteManifest "{{site.data.urls.SkillManifest}}" --cs --luisFolder "<PATH_TO_LU_FOLDER>"
```

The `--luisFolder` parameter can be used to point the Skill CLI at the source LU files for trigger utterances (defaults to `deployment/resources/skills/` inside your assistant folder). The CLI will automatically traverse locale folder hierarchies. Also, you have to specify the `--cs` (for C#) or `--ts` (for TypeScript) argument for determining the coding language of your assistant, since each language takes different folder structures that need to be taken into consideration.

> Note: The Skills can be found in the [botframework-skills](https://github.com/microsoft/botframework-skills) repository.

Once the connect command finish successfully, you can see under the `botFrameworkSkills` property of your Virtual Assistant's appsettings.json file that the following structure was added with the information provided in the Skill manifest.

```json
    "botFrameworkSkills": {
        "id": "<SKILL_ID>",
        "appId": "<SKILL_APPID>",
        "skillEndpoint": "<SKILL_ENDPOINT>",
        "name": "<SKILL_NAME>",
        "description": "<SKILL_DESCRIPTION>"
    },
    "skillHostEndpoint": "<VA_SKILL_ENDPOINT>"
```

See the [Skill CLI documentation]({{site.baseurl}}/skills/handbook/botskills) for detailed CLI documentation.

## Authentication Connection configuration

If a Skill requires Authentication connections to Office/Office 365 this is completed as part of the Skill deployment. No authentication configuration is now required at the Assistant layer.

## Remove a Skill from your Virtual Assistant

To disconnect a skill from your Virtual Assistant use the following command, passing the id of the Skill as per the manifest (e.g. calendarSkill). Also, you have to specify the `--cs` (for C#) or `--ts` (for TypeScript) argument for determining the coding language of your assistant, since each language takes different folder structures that need to be taken into consideration. You can use the `botskills list` to view the registered skills.

```bash
botskills disconnect --skillId <YOUR_SKILL_ID> --cs
```
*Bear in mind that the skillId parameter is case sensitive*

> Note: The id of the Skill can also be aquired using the `botskills list` command. You can check the [Skill CLI documentation]({{site.baseurl}}/skills/handbook/botskills) on this command.

## Updating an existing Skill to reflect changes to Actions or LUIS model

To update a Skill to your assistant/Bot we provide a `botskills` command line tool to automate the process of adding the Skill to your dispatch model and creating authentication connections where needed.

Run the following command to update a Skill to your Virtual Assistant. This assumes you are running the CLI within the project directory and have created your Bot through the template and therefore have a `appsettings.json` file present in the working folder.

```bash
botskills update --remoteManifest "{{site.data.urls.SkillManifest}}" --cs --luisFolder "<PATH_TO_LU_FOLDER>"
```

The `--luisFolder` parameter can be used to point the Skill CLI at the source LU files for trigger utterances. For Skills provided within this repo these can be found in the `Deployment/Resources/LU` folder of each Skill. The CLI will automatically traverse locale folder hierarchies. Also, you have to specify the `--cs` (for C#) or `--ts` (for TypeScript) argument for determining the coding language of your assistant, since each language takes different folder structures that need to be taken into consideration.

> Note: The Skills can be found in the [botframework-skills](https://github.com/microsoft/botframework-skills) repository.

## Refresh Connected Skills

To refresh the dispatch model with any changes made to connected skills use the following command, specifying the `--cs` (for C#) or `--ts` (for TypeScript) argument for determining the coding language of your assistant, since each language takes different folder structures that need to be taken into consideration. 

```bash
botskills refresh --cs
```
