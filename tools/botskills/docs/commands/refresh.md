# Refresh Connected Skills

The `refresh` command allows you to train and publish your existing dispatch model of your **Virtual Assistant**, specifying the Virtual Assistant's coding language using `--cs` or `--ts`. This functionality is mainly useful after using the `connect` or `disconnect` command with the `--noRefresh` flag.

> It's highly advisable to execute this command from the **root folder of your Virtual Assistant bot**, so if you are using the suggested folder structure from the Templates, you may omit most of the optional arguments, as they default to the expected values from the Template's folder structure.

The basic command to refresh your connected skills:

```bash
botskills refresh [options]
```

## Options

| Option | Description |
| - | - |
| --cs | Determine your Virtual Assistant project structure to be a csharp-like structure |
| --ts | Determine your Virtual Assistant project structure to be a TypeScript-like structure |
| --dispatchFolder [path] | (OPTIONAL) Path to the folder containing your Virtual Assistant's `.dispatch` file (defaults to `./deployment/resources/dispatch` inside your Virtual Assistant folder) |
| --outFolder [path] | (OPTIONAL) Path for any output file that may be generated (defaults to your Virtual Assistant's root folder) |
| --lgOutFolder [path] | (OPTIONAL) Path for the Luis Generate output (defaults to a `service` folder inside your Virtual Assistant's folder) |
| --cognitiveModelsFile [path] | (OPTIONAL) Path to your Cognitive Models file (defaults to `cognitivemodels.json` inside your Virtual Assistant's folder) |
| --verbose | (OPTIONAL) Output detailed information about the processing of the tool |
| -h, --help | Output usage information |

Here is an example:

```bash
botskills refresh --cs
```