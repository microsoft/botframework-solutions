# Listing connected skills

The list command allows you to aknowledge the Skills currently connected to your assistant.

> **Tip:** It's highly adviceable to execute this command from the root folder of your assistant bot, so if you are using the suggested folder structure from the Templates, you may ommit most of the optional arguments, as they default to the expected values from the Templates' folder structure.

To acces the list of connected skills:
```bash
botskills list [options]
```

### Options

| Option                   | Description                                                             |
|--------------------------|-------------------------------------------------------------------------|
| -f, --skillsFile [path]  | (OPTIONAL) Path to the assistant Skills configuration file              |
| --verbose                | (OPTIONAL) Output detailed information about the processing of the tool |
| -h, --help               | Output usage information                                                |
