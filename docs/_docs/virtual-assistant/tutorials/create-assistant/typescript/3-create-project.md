---
layout: tutorial
category: Virtual Assistant
subcategory: Create
language: TypeScript
title: Create your assistant
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Create your Virtual Assistant project

Install the botbuilder-assistant generator

```bash
    npm install -g generator-botbuilder-assistant
```

Now you can execute the Virtual Assistant generator with this command.

```bash
yo botbuilder-assistant
```

**At this point you can proceed with two different options:**

### Generate the assistant using prompts

- The generator will start prompting for some information that is needed for generating the sample:
  - `What's the name of your assistant? (customAssistant)`
        > The name of your assistant (also used as your project's name and for the root folder's name).
    - `What's the description of your assistant? ()`
        > The description of your assistant.
    - `Which languages will your assistant use? (by default takes all the languages)`
      - [x] Chinese (`zh`)
      - [x] Deutsch (`de`)
      - [x] English (`en`)
      - [x] French (`fr`)
      - [x] Italian (`it`)
      - [x] Spanish (`es`)
    - `Do you want to change the new assistant's location?`
        > A confirmation to change the destination for the generation.
    - `Where do you want to generate the assistant? (By default, it takes the path where you are running the generator)`
      > The destination path for the generation.
    - `Looking good. Shall I go ahead and create your new assistant?`
        > Final confirmation for creating the desired assistant.

### Generate the sample using CLI parameters

| Option                            | Description                                                                                                  |
|-----------------------------------|--------------------------------------------------------------------------------------------------------------|
| -n, --assistantName <name>              | name of new assistant (by default takes `customAssistant`)                                                          |
| -d, --assistantDesc <description>       | description of the new assistant (by default is empty) |
| -l, --assistantLang <array of languages>| languages for the new assistant. Possible values are `de`, `en`, `es`, `fr`, `it`, `zh` (by default takes all the languages)|
| -p, --assistantGenerationPath <path>    | destination path for the new assistant (by default takes the path where you are running the generator)            |
| --noPrompt                        | indicates to avoid the prompts                                                                               |

**NOTE:** If you don't use the _--noPrompt_ option, the process will keep prompting, but will still be using the input values by default.

#### Example

```bash
> yo botbuilder-assistant -n "Virtual Assistant" -d "A description for my new assistant" -l "en,es" -p "\aPath" --noPrompt
```

After this, you can check the summary on your screen:

```bash
- Name: <aName>
- Description: <aDescription>
- Selected languages: <languages>
- Path: <aPath>
```

>**WARNING:** The process will fail if it finds another folder with the same name of the new assistant.

>**NOTE:** Remember to have an **unique** assistant's name for deployment steps.

You now have your own Virtual Assistant! Before trying to run your assistant locally, continue with the deployment steps (it creates vital dependencies required to run correctly).