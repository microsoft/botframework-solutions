# Refresh Connected Skills

The `refresh` command allows you to refresh the model of your connected skills specifying the assistant's coding language using `--cs` or `--ts`.

> **Tip:** It's highly advisable to execute this command from the **root folder of your assistant bot**, so if you are using the suggested folder structure from the Templates, you may ommit most of the optional arguments, as they default to the expected values from the Templates' folder structure.

The basic command to refresh your connected skills:

```bash
botskills refresh [options]
```

### Options

| Option                        | Description                                                                                                                                                 |
|-------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| --cs                          | Determine your assistant project structure to be a csharp-like structure                                                                                    |
| --ts                          | Determine your assistant project structure to be a TypeScript-like structure                                                                                |
| --dispatchName [name]         | (OPTIONAL) Name of your assistant's '.dispatch' file (defaults to the name displayed in your Cognitive Models file)                                         |
| --language [language]         | (OPTIONAL) Locale used for LUIS culture (defaults to 'en-us')                                                                                               |
| --luisFolder [path]           | (OPTIONAL) Path to the folder containing your Skills' '.lu' files (defaults to './deployment/resources/skills/en' inside your assistant folder)             |
| --dispatchFolder [path]       | (OPTIONAL) Path to the folder containing your assistant's '.dispatch' file (defaults to './deployment/resources/dispatch/en' inside your assistant folder)  |
| --outFolder [path]            | (OPTIONAL) Path for any output file that may be generated (defaults to your assistant's root folder)                                                        |
| --lgOutFolder [path]          | (OPTIONAL) Path for the LuisGen output (defaults to a 'service' folder inside your assistant's folder)                                                      |
| --cognitiveModelsFile [path]  | (OPTIONAL) Path to your Cognitive Models file (defaults to 'cognitivemodels.json' inside your assistant's folder)                                           |
| --verbose                     | (OPTIONAL) Output detailed information about the processing of the tool                                                                                     |
| -h, --help                    | Output usage information                                                                                                                                    |

An example on how to use:

```bash
botskills refresh --cs --verbose
```