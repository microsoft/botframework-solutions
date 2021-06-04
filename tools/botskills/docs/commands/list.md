# List Connected Skills

The `list` command allows you to acknowledge the Skills currently connected to your Virtual Assistant.

> It's highly advisable to execute this command from the **root folder of your Virtual Assistant bot**, so if you are using the suggested folder structure from the Templates, you may omit most of the optional arguments, as they default to the expected values from the Template's folder structure.

The basic command to list the Skills connected to your Virtual Assistant:

```bash
botskills list [options]
```

## Options

| Option | Description |
| - | - |
| --appSettingsFile [path] | (OPTIONAL) Path to your appsettings file where the skills are stored (defaults to `appsettings.json` inside your Virtual Assistant's folder) |
| --verbose | (OPTIONAL) Output detailed information about the processing of the tool |
| -h, --help | Output usage information |

Here is an example:

```bash
botskills list
```