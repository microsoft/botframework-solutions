# Botskills Command Line tool
The Botskills tool is a command line tool to manage the skills connected to your assistant solution.

## Prerequisite
- [Node.js](https://nodejs.org/) version 10.8 or higher
- Install the LUDown and LUISGen CLI tools

```shell
npm install -g ludown luisgen
```

## Installation
To install using npm
```bash
npm install -g botskills
```
This will install botskills into your global path.
To uninstall using npm
```bash
npm uninstall -g botskills
```

## Botskills functionality
- [Connect](./docs/connect-disconnect.md) a Skill to your assistant
- [Disconnect](./docs/connect-disconnect.md) a Skill from your assistant
- [List](./docs/list.md) all Skills connected to your assistant

## Daily builds
Daily builds are based on the latest development code which means they may or may not be stable and probably won't be documented. These builds are better suited for more experienced users and developers although everyone is welcome to give them a shot and provide feedback.

You can get the latest daily build of Botskills from the [BotBuilder MyGet]() feed. To install the daily
```bash
npm install -g botskills --registry https://botbuilder.myget.org/F/aitemplates/npm/
```

## Further Reading
- [Create and customize Skills for your assistant](../../../docs/tutorials/typescript/skill.md)
- [Connect a Skill to your Assistant](../../../docs/howto/skills/addingskills.md)
