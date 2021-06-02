---
category: Skills
subcategory: Handbook
title: Skill CLI Tool
description: Details on usage and commands.
order: 2
toc: true
---

# {{ page.title }}
{:.no_toc}

Botskills command line tool allows you to automate the connection between the **Virtual Assistant** and your **Skills**, which includes the process of updating your dispatch models and create authentication connections where needed.
The CLI performs the following operations on your behalf:
1. Retrieve the **Skill Manifest** from the remote Skill through the `/manifest/manifest-1.1.json` endpoint. If it is a local Skill you should specify the path.
2. Identify which **Language Models** are used by the Skill and resolve the triggering utterances either through local LU file resolution.
3. Add a new dispatch target using the `dispatch` tool to trigger the utterances retrieved in the previous step.
4. Refresh the dispatch LUIS model with the new utterances.

> Your Virtual Assistant must have been deployed using the [deployment tutorial]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/4-provision-your-azure-resources) before using the `botskills` CLI as it relies on the Dispatch models being available and a deployed Bot for authentication connection information.

## Prerequisites
- Download and install [Node Package manager](https://nodejs.org/en/).
   > Node version 10.14.1 or higher is required for the Bot Framework CLI
- Install the Dispatch and botframework-cli

    ```shell
    npm install -g botdispatch @microsoft/botframework-cli
    ```
- [.NET Core runtime](https://dotnet.microsoft.com/download/dotnet-core/2.1#runtime-2.1.0): ^2.1.0
- Install the `botskills` CLI
    ```shell
    npm install -g botskills
    ```

## Commands
For all of these commands, the tool assumes that you are running the CLI within the **Virtual Assistant project directory** and have created your Bot through the template.

### Connect Skills
{:.no_toc}

The `connect` command allows you to connect a Skill, be it local or remote, to your Virtual Assistant bot. The Skill and Virtual Assistant can be in different coding languages without problem, this is, you can connect a Skill coded in C# into a Virtual Assistant coded in TypeScript, but be sure to specify your Virtual Assistant's coding language using `--cs` or `--ts`.

Here is an example:
```bash
botskills connect --remoteManifest "{{site.data.urls.SkillManifest}}" --cs --luisFolder "<PATH_TO_LU_FOLDER>"
```

*Remember to re-publish your Assistant to Azure after you've added a Skill unless you plan on testing locally only*

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

For further information, see the [Connect command documentation]({{site.repo}}/tree/master/tools/botskills/docs/commands/connect.md).

### Disconnect Skills
{:.no_toc}

The `disconnect` command allows you to disconnect a Skill from your Virtual Assistant. You can always check the Skills already connected to your Virtual Assistant using the [`list` command](#list-connected-skills). Remember to specify the coding language of your Virtual Assistant using `--cs` or `--ts`.

Here is an example:
```bash
botskills disconnect --skillId <YOUR_SKILL_ID> --cs
```
*Bear in mind that the skillId parameter is case sensitive*

For further information, see the [Disconnect command documentation]({{site.repo}}/tree/master/tools/botskills/docs/commands/disconnect.md).

> Note: The id of the Skill can also be aquired using the `botskills list` command. You can check the [List command documentation]({{site.repo}}/tree/master/tools/botskills/docs/commands/list.md).

### Update a connected Skill
{:.no_toc}

The `update` command allows you to update a Skill, be it local or remote, to your Virtual Assistant bot. The Skill and Virtual Assistant can be in different coding languages without problem, this is, you can update a Skill coded in C# into a Virtual Assistant coded in TypeScript, but be sure to specify your Virtual Assistant's coding language using `--cs` or `--ts`.

Here is an example:
```bash
botskills update --remoteManifest "{{site.data.urls.SkillManifest}}" --cs --luisFolder "<PATH_TO_LU_FOLDER>"
```

For further information, see the [Update command documentation]({{site.repo}}/tree/master/tools/botskills/docs/commands/update.md).

### Refresh connected Skills
{:.no_toc}

The `refresh` command allows you to train and publish your existing dispatch model of your **Virtual Assistant**, specifying the Virtual Assistant's coding language using `--cs` or `--ts`. This functionality is mainly useful after using the `connect` or `disconnect` command with the `--noRefresh` flag.

Here is an example:
```bash
botskills refresh --cs
```

For further information, see the [Refresh command documentation]({{site.repo}}/tree/master/tools/botskills/docs/commands/refresh.md).

### List connected Skills
{:.no_toc}

The `list` command allows you to acknowledge the Skills currently connected to your Virtual Assistant.

Here is an example:
```bash
botskills list
```

For further information, see the [List command documentation]({{site.repo}}/tree/master/tools/botskills/docs/commands/list.md).

### Migrate Skills

The `migrate` command allows you to transfer all the skills currently connected to your assistant to the new schema configuration settings.

Here is an example:
```bash
botskills migrate --sourceFile "<YOUR-ASSISTANT_PATH>/skills.json" --destFile "<YOUR-ASSISTANT_PATH>/appsettings.json"
```

For further information, see the [Migrate command documentation]({{site.repo}}/tree/master/tools/botskills/docs/commands/migrate.md).
