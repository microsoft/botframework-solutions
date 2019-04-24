# ![Conversational AI Solutions](/docs/media/conversationalai_solutions_header.png)

# Virtual Assistant Solution Accelerator

The Virtual Assistant Solution Accelerator enables customers and partners to quickly build high quality conversational experiences based on the Bot Framework. We bring together the required capabilities provide a solid foundation of the best practices and services needed to create a high quality conversational experience, reducing your effort and enabling you to focus on your own experience.

The Solution Accelerator brings together the following capabilities:
- Virtual Assistant Template (formerly Enterprise Template)
- Conversational AI Skill support enabling Conversational Experiences to leverage conversational building blocks or hand-off specific tasks to *child* Bots within your organisation.
- Out-of-the-Box Skills (e.g. Productivity, Point Of Interest) supplied with full LUIS based language models, dialogs and integration code
- Supporting capabilities such as Linked Accounts and Device integration.

## Virtual Assistant Documentation
The documentation outline for the Virtual Assistant solution accelerator is shown below. C# and Typescript programming languages are supported.

|Documentation|Description|common|csharp|typescript|
|-------|-------|-------|-------|-------|
|Overview| Architecture and Principles|[View](./common/overview.md)|||
|Getting Started|Creating your assistant using the Virtual Assistant template||[View](./csharp/gettingstarted.md)|[View](./typescript/gettingstarted.md)|
|Adding Skills|Adding the out of the box Skills to your Virtual Assistant|[View](./common/addingskills.md)|||
|Project Structure|Walkthrough of your Assistant project||:runner:|:runner:|
|Under the covers|Detailed documentation covering what the template provides and how it works|:runner:|||
|Customizing your assistant|Personalize your assistant||:runner:|:runner:|
|Migration from Enterprise Template|Guidance on how to move from an Enterprise Template based Bot to the new Template||:runner:||Migration from the old Virtual Assistant solution|Guidance on how to move from the original Virtual Assistant solution to the new Template||:runner:|
|Authentication|Authentication approach|[View](./common/authentication.md)|||
|Responses|Types of responses|[View](./common/responses.md)|||
|Testing|Testing steps||[View](./csharp/testing.md)||
|Events|Event handling||[View](./csharp/events.md)|| 
|Device Integration|Device integration examples|[View](./common/deviceintegration.md)||          
|Proactive Messaging|Adding proactive experiences to your assistant||[View](./csharp/proactivemessaging.md)||
|Linked Accounts|Enable users to link 3rd party accounts (e.g. o365) to their assistant||[View](./csharp/linkedaccounts.md)||
|Known Issues|Our current known issues||:runner:|:runner:|

## Skills Documentation
The documentation outline for the preview Conversational AI Skills capability is shown below. C# and Typescript programming languages are supported.

|Documentation|Description|common|csharp|typescript|
|-------|-------|-------|-------|-------|
|Skills Overview|Overview|[View](/docs/skills/csharp/README.md)|||
|Creating a new skill|Creating a new skill using template||[View](/docs/skills/csharp/gettingstarted.md)|[View](/docs/skills/typescript/gettingstarted.md)|
|Adding a new skill to solution| Adding a skill|[View](/docs/skills/common/addingskill.md)||
|Skills Architecture|Architecture|:runner:||
|Skills Under the covers| SkillDialog, Adapter, Middleware||:runner:|:runner:
|Parent Bot to Skill Authentication|Principles, Flow|:runner:||                    
|Skill Token Flow|How a Skill can request a User authentication token||[View](/docs/skills/common/skilltokenflow.md)||
|Skill Manifest| Overview of the Skill Manifest file and it's role with Skill registration and invocation|:runner:||
|Skill CLI | Skill CLI, what it does under covers|:runner:||
|Speech Enablement|SpeechUtility,etc.|:runner:||
|Adaptive Card Styling|Adjusting look/feel - design packs?|:runner:||
|Adding Skill support to a v4 SDK Bot|How to add Skills to an existing/non VA template solution||[View](/docs/skills/csharp/addskillsupportforv4bot.md)|:runner:
|Skill enabling an existing v4 SDK Bot|Steps required to take an existing v4 Bot and make it available as a skill||[View](/docs/skills/csharp/skillenablingav4bot.md)|:runner:
|Preview Limitations / Known Issues||:runner:|:runner:|:runner:

:runner: is work in progress

# Contributing
This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.
When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.
