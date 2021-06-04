# Disconnect a Skill from your Virtual Assistant

The `disconnect` command allows you to disconnect a Skill from your Virtual Assistant. You can always check the Skills already connected to your Virtual Assistant using the [`list` command](./list.md). Remember to specify the coding language of your Virtual Assistant using `--cs` or `--ts`.

> It's highly advisable to execute this command from the **root folder of your Virtual Assistant bot**, so if you are using the suggested folder structure from the Templates, you may omit most of the optional arguments, as they default to the expected values from the Template's folder structure.

The basic command to disconnect a Skill from your Virtual Assistant:

```bash
botskills disconnect [options]
```

## Options

| Option | Description |
| - | - |
| -i, --skillId \<id> | Id of the skill to remove from your Virtual Assistant (case sensitive) |
| --cs | Determine your Virtual Assistant project structure to be a csharp-like structure |
| --ts | Determine your Virtual Assistant project structure to be a TypeScript-like structure |
| --noRefresh [true\|FALSE] | (OPTIONAL) Determine whether the skills models connected to the Virtual Assistant are not going to be trained and published in an existing dispatch model (by default they are trained) |
| --languages [languages] | (OPTIONAL) Comma separated list of locales used for LUIS culture (defaults to `en-us`) |
| --dispatchFolder [path] | (OPTIONAL) Path to the folder containing your Virtual Assistant's `.dispatch` file (defaults to `./deployment/resources/dispatch` inside your Virtual Assistant folder) |
| --outFolder [path] | (OPTIONAL) Path for any output file that may be generated (defaults to your Virtual Assistant's root folder) |
| --lgOutFolder [path] | (OPTIONAL) Path for the Luis Generate output (defaults to a `service` folder inside your Virtual Assistant's folder) |
| --appSettingsFile [path] | (OPTIONAL) Path to your appsettings file where the skills are stored (defaults to `appsettings.json` inside your Virtual Assistant's folder) |
| --cognitiveModelsFile [path] | (OPTIONAL) Path to your Cognitive Models file (defaults to `cognitivemodels.json` inside your Virtual Assistant's folder) |
| --verbose | (OPTIONAL) Output detailed information about the processing of the tool |
| -h, --help | Output usage information |

Here is an example:

```bash
botskills disconnect --skillId "<YOUR_SKILL_ID>" --cs
```