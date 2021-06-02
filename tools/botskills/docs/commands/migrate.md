# Migrate Connected Skills

The `migrate` command allows you to transfer all the skills currently connected to your assistant to the new schema configuration settings.

> It's highly advisable to execute this command from the **root folder of your Virtual Assistant bot**, so if you are using the suggested folder structure from the Templates, you may omit most of the optional arguments, as they default to the expected values from the Template's folder structure.

The basic command to migrate the Skills connected to your Virtual Assistant:

```bash
botskills migrate [options]
```

## Options

| Option | Description |
| - | - |
| --sourceFile [path] | (OPTIONAL) Path to your skills file, which contains the skills that will be migrated (defaults to `skills.json` inside your Virtual Assistant's folder) |
| --destFile [path] | (OPTIONAL) Path to your appsettings file. The skills' information will be migrated to this file (defaults to `appsettings.json` inside your Virtual Assistant's folder) |
| --verbose | (OPTIONAL) Output detailed information about the processing of the tool |
| -h, --help | Output usage information | 

Here is an example:

```bash
botskills migrate --sourceFile "<PATH_TO_SKILLS_FILE>" --destFile "<PATH_TO_APPSETTINGS_FILE>"
```