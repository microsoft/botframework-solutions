# Enterprise Bot Generator
> Project template for advanced bot scenarios using the Bot Builder SDK V4.

## Prerequisites

- Ensure the [Node Package manager](https://nodejs.org/en/) is installed.

- Install [Yeoman](http://yeoman.io) using npm:

```bash
> npm install -g yo
```

## Installation

To install the generator using npm:

```bash
> npm install -g generator-botbuilder-enterprise
```

## Generate sample

- Open a terminal in the desired folder for generating the sample.
- Run the following command for generating your new project.

```bash
> yo botbuilder-enterprise
```

- The generator will start prompting for some information that is needed for generating the sample:
    - `What's the name of your bot? (enterprise-bot)`
        > The name of your bot (used also as your project's name and for the root folder's name).
    - `What will your bot do? (Demonstrate advanced capabilities of a Conversational AI bot)`
        > The description of your bot.
    - `What language will your bot use?`
        > The language that will understand your bot while chatting with it. A full list of supported languages is displayed.
    - `Do you want to change the location of the generation?`
        > A confirmation to change the destination for the generation.
        - `Where do you want to generate the bot? (by default takes the path where you are running the generator)`
            > The destination path for the generation.
    - `Looking good. Shall I go ahead and create your new bot?`
        > Final confirmation for creating the desired bot.


### Generate the sample using CLI parameters.

| Option                            | Description                                                                                                  |
|-----------------------------------|--------------------------------------------------------------------------------------------------------------|
| -n, --botName <name>              | name of new bot (by default takes `enterprise-bot`)                                                          |
| -d, --botDesc <description>       | description of the new bot (by default takes `Demonstrate advanced capabilities of a Conversational AI bot`) |
| -l, --botLang <language>          | language for the new bot. Possible values are `de`, `en`, `es`, `fr`, `it`, `zh` (by default takes `en`)     |
| -p, --botGenerationPath <path>    | destination path for the new bot (by default takes the path where you are runnning the generator)            |
| --noPrompt                        | indicates to avoid the prompts                                                                               |

**NOTE:** If you don't use the _--noPrompt_ option, the process will keep prompting, but using the input values by default.

#### Example

```bash
> yo botbuilder-enterprise -n newBot -d "A description for my new bot" -l en -p "\aPath" --noPrompt
```

**WARNING:** The process will fail if it finds another folder with the same name of the new bot.

**NOTE:** Remind to have an **unique** bot's name for deployment steps. 

**NOTE:** After generating your sample, you can check its README for more information on how to deploy and test it. You can find it in the root folder of your newly created sample or [here](https://github.com/Microsoft/AI/blob/master/templates/Enterprise-Template/src/typescript/enterprise-bot/README.md).

## License

MIT © [Microsoft](http://dev.botframework.com)
