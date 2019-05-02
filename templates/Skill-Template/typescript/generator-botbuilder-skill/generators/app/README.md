# Bot Builder Skill Generator

## Generate skill

- Open a terminal in the desired folder for generating the skill.
- Run the following command for generating your new project.

```bash
> yo botbuilder-skill
```

#### **At this point you have two different options to procedure**

### Generate the skill using prompts

- The generator will start prompting for some information that is needed for generating the skill:
    - `What's the name of your skill? (customSkill)`
        > The name of your skill (used also as your project's name and for the root folder's name).
    - `What will your skill do? ()`
        > The description of your skill.
    - `Do you want to change the new skill's location?`
        > A confirmation to change the destination for the generation.
        - `Where do you want to generate the skill? (by default takes the path where you are running the generator)`
            > The destination path for the generation.
    - `Looking good. Shall I go ahead and create your new skill?`
        > Final confirmation for creating the desired skill.

### Generate the skill using CLI parameters

| Option                            | Description                                                                                                  |
|-----------------------------------|--------------------------------------------------------------------------------------------------------------|
| -n, --skillName <name>            | name of new skill (by default takes `customSkill`)                                                           |
| -d, --skillDesc <description>     | description of the new skill                                                                                 |
| -p, --skillGenerationPath <path>  | destination path for the new skill (by default takes the path where you are runnning the generator)          |
| --noPrompt                        | indicates to avoid the prompts                                                                               |

**NOTE:** If you don't use the _--noPrompt_ option, the process will keep prompting, but using the input values by default.

#### Example

```bash
> yo botbuilder-skill -n newSkill -d "A description for my new skill" -p "\aPath" --noPrompt
```

After this, you can check the summary in your screen:
```bash
- Name: <aName>
- Description: <aDescription>
- Path: <aPath>
```

**WARNING:** The process will fail if it finds another folder with the same name of the new skill.

## License

MIT © [Microsoft](http://dev.botframework.com)