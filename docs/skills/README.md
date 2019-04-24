![Conversational AI Solutions](/docs/media/conversationalai_solutions_header.png)

# Skills Overview

A Skill is like a standard conversational bot but with the ability to be plugged in to a broader solution. This can be a complex Virtual Assistant or perhaps an Enterprise Bot seeking to stitch together multiple bots within an organization.

Apart from some minor differences that enable this special invocation pattern, a Skill looks and behaves like a regular bot. The same protocol is maintained between two bots to ensure a consistent approach. Skills for common scenarios like productivity and navigation to be used as-is or customized however a customer prefers.

## Table of Contents
- [Skills Overview](#skills-overview)
  - [Table of Contents](#table-of-contents)
  - [Skills Documentation](#skills-documentation)
  - [Available Skills](#available-skills)

## Skills Documentation
The documentation outline for the preview Conversational AI Skills capability is shown below. C# and Typescript programming languages are supported.

|Documentation|Description|common|csharp|typescript|
|-------|-------|-------|-------|-------|
|Creating a new Skill|Creating a new Skill using the template||[View](/docs/skills/csharp/gettingstarted.md)|[View](/docs/skills/typescript/gettingstarted.md)|
|Adding a new Skill to solution| Adding a Skill|[View](/docs/skills/common/addingskill.md)||
|Skills Architecture|Architecture|[View](/docs/skills/common/architecture.md)||
|Best Practices for your Skill|Architecture||[View](/docs/skills/csharp/bestpractices.md)|:runner:
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

## Available Skills

The following Skills are available out of the box, each of the documentation links below has the deployment steps required to deploy and configure Skills for your use.

- [Productivity - Calendar](./productivity-calendar.md)
- [Productivity - Email](./productivity-email.md)
- [Productivity - To Do](./productivity-todo.md)
- [Point of Interest](./pointofinterest.md)
- [Automotive](./automotive.md)
- [Experimental Skills](./experimental-skills.md)