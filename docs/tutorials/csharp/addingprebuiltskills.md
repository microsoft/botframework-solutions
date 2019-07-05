# Adding Pre-Built Skills (C#/Typescript)

**APPLIES TO:** ✅ SDK v4

## In this tutorial
- [Intro](#intro)
- [Download and install](#download-and-install)
- [Choose which Skill you wish to use](#Choose-which-Skill-you-wish-to-use)
- [Adding your Skill to an assistant](#Adding-your-Skill-to-an-assistant)
- [Testing your Skill](#Testing-your-Skill)

## Intro

### Purpose

Install Bot Framework development prerequisites and add one of the Skills provided as part of the Virtual Assistant.

### Prerequisites

- [Create a Virtual Assistant](/docs/tutorials/csharp/virtualassistant.md) to setup your environment.

### Time to Complete

15 minutes

### Scenario

Add one of the skills provided in the [Bot Framework Solutions GitHub repo](https://github.com/microsoft/botframework-solutions) provide to your Virtual Assistant. The Skills are only available in C# at this time but these can be added to a Typescript based assistant.

## Download and install

> It's important to ensure all of the following pre-requisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Ensure you have updated [.NET Core](https://www.microsoft.com/net/download) to the **latest** version.  
2. Ensure the [Node Package manager](https://nodejs.org/en/) is installed.
3. PowerShell Core version 6 (Required for cross platform deployment support)
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on macOS and Linux](https://aka.ms/getps6-linux)
4. Install  Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions as we make use of the latest capabilities:

   ```
   npm install -g botdispatch ludown luis-apis qnamaker luisgen@2.0.2
   ```

5. Install Botskills (CLI) tool:
   
   ```
   npm install -g botskills
   ```

6. Install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)

## Choose which Skill you wish to use.

Choose which of the provided Skills you wish to add to your Virtual Assistant, click one of the links below and follow the **`Skill Deployment`** instructions to deploy your own instance of this Skill.

| Name | Description |
| ---- | ----------- |
|[Calendar Skill](/docs/reference/skills/productivity-calendar.md)|Add calendar capabilities to your assistant. Powered by Microsoft Graph and Google.|
|[Email Skill](/docs/reference/skills/productivity-email.md)|Add email capabilities to your assistant. Powered by Microsoft Graph and Google.|
|[To Do Skill](/docs/reference/skills/productivity-todo.md)|Add task management capabilities to your assistant. Powered by Microsoft Graph.|
|[Point of Interest Skill](/docs/reference/skills/pointofinterest.md)|Find points of interest and directions. Powered by Azure Maps and FourSquare.|
|[Automotive Skill](/docs/reference/skills/automotive.md)|Industry-vertical Skill for showcasing enabling car feature control.|
|[Experimental Skills](/docs/reference/skills/experimental.md)|News, Search, Restaurant Booking and Weather.|

## Adding your Skill to an assistant

Once you've deployed your Skill you can now add this to your Assistant. 

To add your new Skill to your assistant/Bot we provide a `botskills` command line tool to automate the process of adding the Skill to your dispatch model and creating authentication connections where needed. 

Run the following command from a command prompt **within the directory of your assistant/Bot**. 

```bash
botskills connect --botName YOUR_BOT_NAME --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest" --luisFolder "<YOUR-SKILL_PATH>\Deployment\Resources\LU\en\" --cs
```

**Remember to re-publish your Assistant to Azure after you've added a Skill unless you plan on testing locally only**

See the [Adding Skills](/docs/howto/skills/botskills.md#Connect-Skills) for more detail on how to add skills.

## Testing your Skill

Refer to the Skill documentation page in the table above for an example question that you can ask and validate that your Assistant can now perform additional capabilities with no additional code changes.
