# Enterprise Bot Generator
> Project template for advanced bot scenarios using the Bot Builder SDK V4.

## Prerequisites

- Ensure the [Node Package manager](https://nodejs.org/en/) is installed.

- Install [Yeoman](http://yeoman.io) using npm:

```bash
npm install -g yo
```

## Installation

To install using npm:

```bash
npm install -g generator-botbuilder-enterprise
```

## Generate sample

- Open a terminal in the desired folder for generating the sample.
- Run the following command for generating your new project.

```bash
yo botbuilder-enterprise
```

- The generator will start prompting for some information that is needed for generating the sample:
    - `What's the name of your bot?`
        > The name of your bot (used also as your project's name and for the root folder's name).
    - `What will your bot do?`
        > The description of your bot.
    - `What language will your bot use?`
        > The language that will understand your bot while chatting with it. A full list of supported languages is displayed.
    - `Looking good. Shall I go ahead and create your new bot?`
        > Final confirmation for creating the desired bot.

**NOTE:** After generating your sample, you can read its README for more information on how to deploy and test it. You can find it in the root folder of your newly created sample or [here](https://github.com/Microsoft/AI/blob/master/templates/Enterprise-Template/src/typescript/enterprise-bot/README.md).

## License

MIT © [Microsoft](http://dev.botframework.com)
