# Virtual Assistant Generator
Project template for Virtual Assistants and Skill using the Bot Builder SDK V4.

- [Installing](#installing)
- [GitHub Repo](https://github.com/microsoft/botframework-solutions)
- [Report Issues](https://github.com/microsoft/botframework-solutions/issues)

## Prerequisites
- Download and install [Node Package manager](https://nodejs.org/en/).
   > Node version 10.14.1 or higher is required for the Bot Framework CLI

- Install [Yeoman](http://yeoman.io) using npm:

```bash
npm install -g yo
```

## Installing
To install the generator using npm:

```bash
npm install -g generator-bot-virtualassistant@latest
```

#### How to Use Daily Builds
To get access to the daily builds of this library, configure npm to use the MyGet feed before installing.

```bash
npm config set registry https://botbuilder.myget.org/F/aitemplates/npm/
```

To reset the registry in order to get the latest published version, run:
```bash
npm config set registry https://registry.npmjs.org/
```

## Build and test locally
Install the dependencies and dev dependencies of the project you want to test, i.e. bot-virtualassistant.
```bash
cd ./templates/typescript/generator-bot-virtualassistant
npm install
```

Link the package of the project locally with the following command so you can easily use it globally.
```bash
npm link
```
> You can test your local changes to the generator immediately if using this command.

Now you can execute the generator with this command.
```bash
yo bot-virtualassistant
```

| Generator                                           | Description                                     |
|-----------------------------------------------------|-------------------------------------------------|
| [bot-virtualassistant](https://github.com/microsoft/botframework-solutions/blob/master/templates/typescript/generator-bot-virtualassistant/generators/app/README.md)    | Generator that creates a basic Virtual Assistant        |
| [bot-virtualassistant:skill](https://github.com/microsoft/botframework-solutions/blob/master/templates/typescript/generator-bot-virtualassistant/generators/skill/README.md)    | Generator that creates a basic skill        |

## License
MIT © [Microsoft](http://dev.botframework.com)