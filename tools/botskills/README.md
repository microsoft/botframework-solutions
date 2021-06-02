# Botskills Command Line Tool
The Botskills Tool is a command line tool to manage the Skills connected to your Virtual Assistant solution.

- [Installing](#installing)
- [GitHub Repo](https://github.com/microsoft/botframework-solutions)
- [Report Issues](https://github.com/microsoft/botframework-solutions/issues)

## Prerequisite
- Download and install [Node Package manager](https://nodejs.org/en/).
   > Node version 10.14.1 or higher is required for the Bot Framework CLI
- Install [@microsoft/botframework-cli](https://www.npmjs.com/package/@microsoft/botframework-cli)


```shell
npm install -g botdispatch @microsoft/botframework-cli
```
- [.NET Core runtime](https://dotnet.microsoft.com/download/dotnet-core/2.1#runtime-2.1.0): ^2.1.0

## Installing
Using npm:
```bash
npm install -g botskills@latest
```

## Commands
- [Connect](https://github.com/microsoft/botframework-solutions/blob/master/tools/botskills/docs/commands/connect.md) a Skill to your assistant
- [Disconnect](https://github.com/microsoft/botframework-solutions/blob/master/tools/botskills/docs/commands/disconnect.md) a Skill from your assistant
- [Update](https://github.com/microsoft/botframework-solutions/blob/master/tools/botskills/docs/commands/update.md) a Skill from your assistant 
- [Refresh](https://github.com/microsoft/botframework-solutions/blob/master/tools/botskills/docs/commands/refresh.md) connected skills
- [List](https://github.com/microsoft/botframework-solutions/blob/master/tools/botskills/docs/commands/list.md) all Skills connected to your assistant
- [Migrate](https://github.com/microsoft/botframework-solutions/blob/master/tools/botskills/docs/commands/migrate.md) all the Skills to the new schema configuration settings

#### How to Use Daily Builds
If you want to play with the very latest versions of bot-solutions, you can opt in to working with the daily builds. This is not meant to be used in a production environment and is for advanced development. Quality will vary and you should only use daily builds for exploratory purposes.

To get access to the daily builds of this library, configure npm to use the MyGet feed before installing.

```bash
npm config set registry https://botbuilder.myget.org/F/aitemplates/npm/
```

To reset the registry in order to get the latest published version, run:
```bash
npm config set registry https://registry.npmjs.org/
```

## Further Reading
- Create and customize Skills for your Virtual Assistant - [C#](https://microsoft.github.io/botframework-solutions/skills/tutorials/create-skill/csharp/1-intro/) & [TypeScript](https://microsoft.github.io/botframework-solutions/skills/tutorials/create-skill/typescript/1-intro/)
- [Skill CLI Tool](https://microsoft.github.io/botframework-solutions/skills/handbook/botskills/)
- [Multilanguage functionality](./docs/multilanguage-functionality.md)
 
## License
MIT © [Microsoft](http://dev.botframework.com)