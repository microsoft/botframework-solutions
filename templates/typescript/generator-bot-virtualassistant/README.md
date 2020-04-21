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
npm install -g generator-bot-virtualassistant
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
> **Note:** You can test your local changes to the generator immediately if using this command.
Now you can execute the generator with this command.
```bash
yo bot-virtualassistant
```

| Generator                                           | Description                                     |
|-----------------------------------------------------|-------------------------------------------------|
| [bot-virtualassistant](generators/app/README.md)    | Generator that creates a basic Virtual Assistant        |
| [bot-virtualassistant:skill](generators/skill/README.md)    | Generator that creates a basic skill        |


## License

MIT © [Microsoft](http://dev.botframework.com)