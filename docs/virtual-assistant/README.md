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
|Overview| Architecture and Principles| [View](./common/overview.md)|||
|Getting Started|Creating your assistant using the Virtual Assistant template||[View](./csharp/gettingstarted.md)|[View](./typescript/gettingstarted.md)|
|Project Structure|Walkthrough of your Assistant project||[View](./csharp/projectstructure.md)|[View](./typescript/projectstructure.md)|
|Under the covers|Detailed documentation covering what the template provides and how it works|[View](./common/underthecovers.md)|||
|Customizing your assistant|Personalize your assistant||[View](./csharp/personalization.md)|:runner:|
|Migration from Enterprise Template|Guidance on how to move from an Enterprise Template based Bot to the new Template||[View](./csharp/personalization.md)||Migration from the old Virtual Assistant solution|Guidance on how to move from the original Virtual Assistant solution to the new Template||[View](./csharp/personalization.md)|
|Authentication|Authentication approach|[View](./common/authentication.md)|||
|Responses|Types of responses|[View](./common/responses.md)|||
|Testing|Testing steps||[View](./csharp/testing.md)||
|Events|Event handling||[View](./csharp/events.md)|| 
|Device Integration|Device integration examples|[View](./common/deviceintegration.md)|||           
|Proactive Messaging|Adding proactive experiences to your assistant||[View](./csharp/proactivemessaging.md)||
|Linked Accounts|Enable users to link 3rd party accounts (e.g. o365) to their assistant||[View](./csharp/linkedaccounts.md)||
|Known Issues|Our current known issues||[View](./csharp/knownissues.md)||

## Skills Documentation
The documentation outline for the preview Conversational AI Skills capability is shown below. C# and Typescript programming languages are supported.

|Documentation|Description|common|csharp|typescript|
|-------|-------|-------|-------|-------|
|Skills Overview|Overview|[View](/docs/skills/common/skillsoverview.md)|||
|Creating a new skill|Creating a new skill using template||[View](/docs/skills/csharp/gettingstarted.md)|[View](/docs/skills/typescript/gettingstarted.md)|
|Adding a new skill to solution| Adding a skill|||
|Skills Architecture|Architecture|||
|Skills Under the covers| SkillDialog, Adapter, Middleware|||
|Parent Bot to Skill Authentication|Principles, Flow|||                    
|Skill Token Flow|How tokens are requested|||
|Manifest | Manifest File, Actions, triggering utterances|||
|Skill CLI | Skill CLI, what it does under covers|||
|Speech Enablement|SpeechUtility,etc.|||
|Adaptive Card Styling|Adjusting look/feel - design packs?|||
|Add SkillSupport to a non VA template project|How to add Skills to an exist/non VA template solution|||
|Turning an existing Bot into a Skill|Steps required to enable Skill mode for existing v4 bots|||
|Preview Limitations / Known Issues       |||



# Contributing
This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.
When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.
