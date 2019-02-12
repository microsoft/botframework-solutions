# Enterprise Bot Generator
> Project template for advanced bot scenarios using the Bot Builder SDK V4.

## Prerequisites

- Ensure the [Node Package manager](https://nodejs.org/en/) is installed.

- Install [Yeoman](http://yeoman.io) using npm:

```bash
npm install -g yo
```

## Installation

To install the generator using npm:

```bash
npm install -g generator-botbuilder-enterprise
```

## Build and test locally

Install the dependencies and dev dependencies of the project you want to test, i.e. botbuilder-enterprise.
```bash
cd ./templates/Enterprise-Template/src/typescript/generator-botbuilder-enterprise/
npm install
```

Link the package of the project locally with the following command so you can easily use it globally.
```bash
npm link
```
> **Note:** You can test your local changes to the generator immediately if using this command.

Now you can execute the generator with this command.
```bash
yo botbuilder-enterprise
```

| Generator                                           | Description                                     |
|-----------------------------------------------------|-------------------------------------------------|
| [botbuilder-enterprise](generators/app/README.md)             | Generator that creates a basic sample            |
| [botbuilder-enterprise:dialog](generators/dialog/README.md)   | Generator that creates a basic dialog            |
| [botbuilder-enterprise:middleware](generators/middleware/README.md)   | Generator that creates a basic middleware        |

## License

MIT © [Microsoft](http://dev.botframework.com)
