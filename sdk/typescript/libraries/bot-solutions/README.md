# Bot-Solutions
Shared library for Conversational AI Virtual Assistants and Skills.

- [Installing](#installing)
- [GitHub Repo](https://github.com/microsoft/botframework-solutions)
- [Report Issues](https://github.com/microsoft/botframework-solutions/issues)

## Installing
To add the latest version of this package to your bot:

```bash
npm install --save bot-solutions
```

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