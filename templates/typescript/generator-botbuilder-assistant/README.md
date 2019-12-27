# Virtual Assistant Generator
> Project template for virtual assistants using the Bot Builder SDK V4..
## Prerequisites

- Ensure the [Node Package manager](https://nodejs.org/en/) is installed.

- Install [Yeoman](http://yeoman.io) using npm:

```bash
npm install -g yo
```

## Installation

To install the generator using npm:

```bash
npm install -g generator-botbuilder-assistant
```

## Build and test locally

Install the dependencies and dev dependencies of the project you want to test, i.e. botbuilder-assistant.
```bash
cd ./templates/Virtual-Assistant-Template/src/typescript/generator-botbuilder-assistant/
npm install
```

Link the package of the project locally with the following command so you can easily use it globally.
```bash
npm link
```
> **Note:** You can test your local changes to the generator immediately if using this command.
Now you can execute the generator with this command.
```bash
yo botbuilder-assistant
```

| Generator                                           | Description                                     |
|-----------------------------------------------------|-------------------------------------------------|
| [botbuilder-assistant](generators/app/README.md)    | Generator that creates a basic assistant        |
| [botbuilder-assistant:skill](generators/skill/README.md)    | Generator that creates a basic skill        |


## License

MIT © [Microsoft](http://dev.botframework.com)