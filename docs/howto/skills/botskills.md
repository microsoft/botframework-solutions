# Botskills

## In this document
- [Botskills](#Botskills)
  - [In this document](#In-this-document)
  - [Overview](#Overview)
  - [Prerequisites](#Prerequisites)
  - [Commands](#Commands)
    - [Connect Skills](#Connect-Skills)
    - [Disconnect Skills](#Disconnect-Skills)
    - [Update a Connected Skill](#Update-a-Connected-Skill)
    - [Refresh Connected Skills](#Refresh-Connected-Skills)
    - [List Connected Skills](#List-Connected-Skills)

## Overview
`Botskills` command line tool allows you to automate the connection between the **Virtual Assistant** and your **Skills**, which includes the process of update your dispatch models and create authentication connections where needed.
The CLI performs the following operations on your behalf:
1. Retrieve the **Skill Manifest** from the local/remote Skill through the `/api/skill/manifest` endpoint.
2. Identify which **Language Models** are used by the Skill and resolve the triggering utterances either through local LU file resolution or through inline trigger utterances if requested.
3. Add a new dispatch target using the `dispatch` tool using the trigger utterances retrieved in the previous step.
4. Refresh the dispatch LUIS model with the new utterances
5. In the case of **Active Directory Authentication Providers**, an authentication connection will be added to your Bot automatically and the associated Scopes added to your Azure AD application that backs your deployed Assistant.

> Your Virtual Assistant must have been deployed using the [deployment tutorial](/docs/tutorials/assistantandskilldeploymentsteps.md) before using the `botskills` CLI as it relies on the Dispatch models being available and a deployed Bot for authentication connection information.

## Prerequisites
- [Node.js](https://nodejs.org/) version 10.8 or higher
- Install the Dispatch, LUDown and LUISGen CLI tools

    ```shell
    npm install -g botdispatch ludown luisgen
    ```
- Install the `botskills` CLI
    ```shell
    npm install -g botskills
    ```

## Commands
For all of this commands, the tool assumes that you are running the CLI within the **Virtual Assistant project directory** and have created your Bot through the template, and therefore have a `skills.json` file present in the working folder which contains the connected skills.

### Connect Skills
The `connect` command allows you to connect a Skill, be it local or remote, to your Virtual Assistant bot. The Skill and Virtual Assistant can be in different coding languages without problem, this is, you can connect a Skill coded in C# into an Virtual Assistant coded in TypeScript, but be sure to specify your Virtual Assistants coding language using `--cs` or `--ts`.

Here is an example:
```bash
botskills connect --botName YOUR_VA_NAME --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --luisFolder [path] --cs
```

*Remember to re-publish your Assistant to Azure after you've added a Skill unless you plan on testing locally only**

For further information, see the [Connect command documentation](/tools/botskills/docs/connect.md).

### Disconnect Skills
The `disconnect` command allows you to disconnect a Skill from your Virtual Assistant. You can always check the Skills already connected to your Virtual Assistant using the [`list` command](./list.md). Remember to specify the coding language of your Virtual Assistant using `--cs` or `--ts`.

Here is an example:
```bash
botskills disconnect --skillId SKILL_ID --cs
```

For further information, see the [Disconnect command documentation](/tools/botskills/docs/disconnect.md).

> Note: The id of the Skill can also be aquired using the `botskills list` command. You can check the [List command documentation](/tools/botskills/docs/list.md).

### Update a Connected Skill
The `update` command allows you to update a Skill, be it local or remote, to your Virtual Assistant bot. The Skill and Virtual Assistant can be in different coding languages without problem, this is, you can update a Skill coded in C# into an Virtual Assistant coded in TypeScript, but be sure to specify your assistants coding language using `--cs` or `--ts`.

Here is an example:
```bash
botskills update --botName YOUR_BOT_NAME --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --luisFolder [path] --cs
```

For further information, see the [Update command documentation](/tools/botskills/docs/update.md).

### Refresh Connected Skills
The `refresh` command allows you to train and publish your existing dispatch model of your **Virtual Assistant**, specifying the Virtual Assistant's coding language using `--cs` or `--ts`.

Here is an example:
```bash
botskills refresh --cs
```

For further information, see the [Refresh command documentation](/tools/botskills/docs/refresh.md).

### List Connected Skills
The `list` command allows you to acknowledge the Skills currently connected to your Virtual Assistant.

Here is an example:
```bash
botskills list
```

For further information, see the [List command documentation](/tools/botskills/docs/list.md).