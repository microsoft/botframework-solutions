# Botskills Command Line Tool
The Botskills Tool is a command line tool to manage the skills connected to your assistant solution.

## Prerequisite
- [Node.js](https://nodejs.org/) version 10.8 or higher
- Install [@microsoft/botframework-cli](https://www.npmjs.com/package/@microsoft/botframework-cli)


```shell
npm install -g botdispatch @microsoft/botframework-cli
```
- [.NET Core runtime](https://dotnet.microsoft.com/download/dotnet-core/2.1#runtime-2.1.0): ^2.1.0

## Installation
Using npm:
```bash
npm install -g botskills@latest
```
This will install botskills into your global path.

To uninstall using npm:
```bash
npm uninstall -g botskills
```

## Commands
- [Connect](./docs/commands/connect.md) a Skill to your assistant
- [Disconnect](./docs/commands/disconnect.md) a Skill from your assistant
- [Update](./docs/commands/update.md) a Skill from your assistant 
- [Refresh](./docs/commands/refresh.md) connected skills
- [List](./docs/commands/list.md) all Skills connected to your assistant
- [Migrate](./docs/commands/migrate.md) all the skills to the new schema configuration settings.

## Daily builds
Daily builds are based on the latest development code which means they may or may not be stable and probably won't be documented. These builds are better suited for more experienced users and developers although everyone is welcome to give them a shot and provide feedback.

You can get the latest daily build of Botskills from the [BotBuilder MyGet](https://botbuilder.myget.org/gallery/aitemplates) feed.
To install the daily execute:
```bash
npm install -g botskills@latest --registry https://botbuilder.myget.org/F/aitemplates/npm/
```

## Further Reading
- Create and customize Skills for your assistant - [C#](https://microsoft.github.io/botframework-solutions/skills/tutorials/create-skill/csharp/1-intro/) & [TypeScript](https://microsoft.github.io/botframework-solutions/skills/tutorials/create-skill/typescript/1-intro/)
- [Connect Skills](https://microsoft.github.io/botframework-solutions/skills/handbook/botskills#Connect-Skills)
- [Disconnect Skills](https://microsoft.github.io/botframework-solutions/skills/handbook/botskills#Disconnect-Skills)
- [Update a Connected Skill](https://microsoft.github.io/botframework-solutions/skills/handbook/botskills#Update-a-Connected-Skill)
- [Refresh Connected Skills](https://microsoft.github.io/botframework-solutions/skills/handbook/botskills#Refresh-Connected-Skills)
- [List Connected Skills](https://microsoft.github.io/botframework-solutions/skills/handbook/botskills#List-Connected-Skills)
- [Multilanguage functionality](./docs/multilanguage-functionality.md)
 
