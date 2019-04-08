# Botskills Command Line tool
The Botskills tool is a command line tool to manage the skills connected to your assistant solution.

## Prerequisite
- [Node.js](https://nodejs.org/) version 10.8 or higher

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
- Connect a skill to your assistant
- List all connected skill to your assistant

## Nightly builds
Nightly builds are based on the latest development code which means they may or may not be stable and probably won't be documented. These builds are better suited for more experienced users and developers although everyone is welcome to give them a shot and provide feedback.

You can get the latest nightly build of Botskills from the [BotBuilder MyGet]() feed. To install the nightly
```bash
npm config set registry https://botbuilder.myget.org/F/botbuilder-ai-daily/npm
```
Install using npm
```bash
npm install -g botskills
```
To reset registry
```bash
npm config set registry https://registry.npmjs.org/
```

## Further Reading
- [Create and customize Skills for your assistant]().