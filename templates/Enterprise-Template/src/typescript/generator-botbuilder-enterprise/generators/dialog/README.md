# Bot Builder Enterprise Dialog Generator

## Generate dialog

- Open a terminal in the desired folder for generating the dialog.
- Run the following command for generating your new component.

```bash
> yo botbuilder-enterprise:dialog
```
#### **At this point you have two different options to procedure**

### Generate the sample using prompts

- The generator will start prompting for some information that is needed for generating the dialog:
    - `What's the name of your dialog? (custom)`
        > The name of your dialog (used also as the root folder's name).
    - `Do you want to change the location of the generation?`
        > A confirmation to change the destination for the generation.
        - `Where do you want to generate the dialog? (by default takes the path where you are running the generator)`
            > The destination path for the generation.
    - `Looking good. Shall i go ahead and create your new dialog?`
        > Final confirmation for creating the desired dialog.

### Generate the dialog using CLI parameters


| Option                               | Description                                                                                                  |
|--------------------------------------|--------------------------------------------------------------------------------------------------------------|
| -n, --dialogName <name>              | name of new dialog (by default takes `custom`)                                                               |
| -p, --dialogGenerationPath <path>    | destination path for the new dialog (by default takes the path where you are runnning the generator)         |
| --noPrompt                           | indicates to avoid the prompts                                                                               |

**NOTE:** If you don't use the _--noPrompt_ option, the process will keep prompting, but using the input values by default.

#### Example

```bash
> yo botbuilder-enterprise:dialog -n newDialog -p "\aPath" --noPrompt
```

After this, you can check the summary in your screen:

```bash
- Folder: <aDialogFolder>
- Dialog file: <aDialogFile> (with .ts extension)
- Responses file: <aResponsesFile> (with .ts extension)
- Path: <aPath>
```

## License

MIT © [Microsoft](http://dev.botframework.com)