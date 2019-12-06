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
1. Retrieve the **Skill Manifest** from the remote Skill through the `/api/skill/manifest` endpoint. If it is a local Skill you should specify the path.
2. Identify which **Language Models** are used by the Skill and resolve the triggering utterances either through local LU file resolution.
3. Add a new dispatch target using the `dispatch` tool to trigger the utterances retrieved in the previous step.
4. Refresh the dispatch LUIS model with the new utterances.
5. In the case of **Active Directory Authentication Providers**, an authentication connection will be added to your Bot automatically and the associated Scopes added to your Azure AD application that backs your deployed Assistant.

> Your Virtual Assistant must have been deployed using the [deployment tutorial]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/4-provision-your-azure-resources) before using the `botskills` CLI as it relies on the Dispatch models being available and a deployed Bot for authentication connection information.

## Prerequisites
- [Node.js](https://nodejs.org/) version 10.8 or higher
- Install the Dispatch, LUDown and LUISGen CLI tools

    ```shell
    npm install -g botdispatch ludown luisgen
    ```
- Install the Botskills CLI tool:
    ```shell
    npm install -g botskills@latest
    ```

## Commands
For all of this commands, the tool assumes that you are running the CLI within the **Virtual Assistant project directory** and have created your Bot through the template, and therefore have a `skills.json` file present in the working folder which contains the connected skills.

### Connect Skills
{:.no_toc}

The `connect` command allows you to connect a Skill, be it local or remote, to your Virtual Assistant bot. The Skill and Virtual Assistant can be in different coding languages without problem, this is, you can connect a Skill coded in C# into a Virtual Assistant coded in TypeScript, but be sure to specify your Virtual Assistant's coding language using `--cs` or `--ts`.

Here is an example:
```bash
botskills connect --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --cs
```

*Remember to re-publish your Assistant to Azure after you've added a Skill unless you plan on testing locally only*

For further information, see the [Connect command documentation]({{site.repo}}/tree/master/tools/botskills/docs/commands/connect.md).

### Disconnect Skills
{:.no_toc}

The `disconnect` command allows you to disconnect a Skill from your Virtual Assistant. You can always check the Skills already connected to your Virtual Assistant using the [`list` command](#List-Connected-Skills). Remember to specify the coding language of your Virtual Assistant using `--cs` or `--ts`.

Here is an example:
```bash
botskills disconnect --skillId <YOUR_SKILL_ID> --cs
```

For further information, see the [Disconnect command documentation]({{site.repo}}/tree/master/tools/botskills/docs/commands/disconnect.md).

> Note: The id of the Skill can also be aquired using the `botskills list` command. You can check the [List command documentation]({{site.repo}}/tree/master/tools/botskills/docs/commands/list.md).

### Update a connected Skill
{:.no_toc}

The `update` command allows you to update a Skill, be it local or remote, to your Virtual Assistant bot. The Skill and Virtual Assistant can be in different coding languages without problem, this is, you can update a Skill coded in C# into a Virtual Assistant coded in TypeScript, but be sure to specify your Virtual Assistant's coding language using `--cs` or `--ts`.

Here is an example:
```bash
botskills update --botName <YOUR_BOT_NAME> --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --cs
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

## Multilanguage Functionality
We introduced the possibility to connect multiple languages of the same Skill at the same time to the Virtual Assistant using Botskills CLI Tool.

Connections depend on the triangulation of the following language sources:
* The `Dispatch models` available in the Virtual Assistant 
* The `languages for the intents` in the [Skill Manifest](https://microsoft.github.io/botframework-solutions/skills/handbook/manifest/)
* The `--languages` argument of the `botskills connect` command

```bash
botskills connect --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest?inlineTriggerUtterances=false" --cs --languages "en-us,es-es"
```

For further information, see the [Multilanguage functionality documentation]({{site.repo}}/tree/master/tools/botskills/docs/multilanguage-functionality.md).