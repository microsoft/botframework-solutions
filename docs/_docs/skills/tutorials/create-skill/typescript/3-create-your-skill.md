---
layout: tutorial
category: Skills
subcategory: Create
language: TypeScript
title: Create your skill project
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

Install the botbuilder-assistant generator

```bash
npm install -g generator-botbuilder-assistant
```

Now you can execute the Skill sub-generator with this command.

```bash
yo botbuilder-assistant:skill
```

**At this point you have two different options to proceed:**

### Generate the skill using prompts

- The generator will start prompting for some information that is needed for generating the sample:
  - `What's the name of your skill? (customSkill)`
    > The name of your skill (used also as your project's name and for the root folder's name).
  - `What's the description of your skill? ()`
    > The description of your skill.
  - `Which languages will your skill use? (by default takes all the languages`
    - [x] Chinese (`zh`)
    - [x] Deutsch (`de`)
    - [x] English (`en`)
    - [x] French (`fr`)
    - [x] Italian (`it`)
    - [x] Spanish (`es`)
  - `Do you want to change the new skill's location?`
    > A confirmation to change the destination for the generation.
  - `Where do you want to generate the skill? (by default takes the path where you are running the generator)`
    > The destination path for the generation.
  - `Looking good. Shall I go ahead and create your new skill?`
    > Final confirmation for creating the desired skill.

### Generate the sample using CLI parameters

| Option                            | Description                                                                                                  |
|-----------------------------------|--------------------------------------------------------------------------------------------------------------|
| -n, --skillName <name>              | name of new skill (by default takes `customSkill`)                                                          |
| -d, --skillDesc <description>       | description of the new skill (by default is empty) |
| -l, --skillLang <array of languages>| languages for the new skill. Possible values are `de`, `en`, `es`, `fr`, `it`, `zh` (by default takes all the languages)|
| -p, --skillGenerationPath <path>    | destination path for the new skill (by default takes the path where you are running the generator)            |
| --noPrompt                        | indicates to avoid the prompts                                                                               |

**NOTE:** If you don't use the _--noPrompt_ option, the process will keep prompting, but using the input values by default.

#### Example

```bash
> yo botbuilder-assistant:skill -n "My skill" -d "A description for my new skill" -l "en" -p "\aPath" --noPrompt
```

After this, you can check the summary in your screen:

```bash
- Name: <aName>
- Description: <aDescription>
- Selected languages: <languages>
- Path: <aPath>
```

>**WARNING:** The process will fail if it finds another folder with the same name of the new skill.

>**NOTE:** Remind to have an **unique** skill's name for deployment steps. 

You now have your own Skill! Follow the Deployment steps below before you try and run the project as deployment creates key dependencies required for operation.
