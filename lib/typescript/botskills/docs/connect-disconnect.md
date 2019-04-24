# Connect a Skill to your assistant

To connect a Skill to your assistant:

```bash
botskills connect [options]
```

### Options

| Option                        | Description                                                                                                                                                 |
|-------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| -b, --botName \<name>         | Name of your assistant bot                                                                                                                                  |
| -l, --localManifest \<path>   | Path to local Skill Manifest file                                                                                                                           |
| -r, --remoteManifest \<url>   | URL to remote Skill Manifest                                                                                                                                |
| --dispatchName [name]         | (OPTIONAL) Name of your assistant's '.dispatch' file (defaults to the name displayed in your Cognitive Models file)                                         |
| --language [language]         | (OPTIONAL) Locale used for LUIS culture (defaults to 'en-us')                                                                                               |
| --luisFolder [path]           | (OPTIONAL) Path to the folder containing your Skills' '.lu' files (defaults to './deployment/resources/skills/en' inside your assistant folder)             |
| --dispatchFolder [path]       | (OPTIONAL) Path to the folder containing your assistant's '.dispatch' file (defaults to './deployment/resources/dispatch/en' inside your assistant folder)  |
| --outFolder [path]            | (OPTIONAL) Path for any output file that may be generated (defaults to your assistant's root folder')                                                       |
| --lgOutFolder [path]          | (OPTIONAL) Path for the LuisGen output (defaults to a 'service' folder inside your assistant's folder')                                                     |
| --skillsFile [path]           | (OPTIONAL) Path to your assistant Skills configuration file (defaults to the 'skills.json' inside your assistant's folder)                                  |
| --resourceGroup [name]        | (OPTIONAL) Name of your assistant's resource group in Azure (defaults to your assistant's bot name)                                                         |
| --appSettingsFile [path]      | (OPTIONAL) Path to your app settings file (defaults to 'appsettings.json' inside your assistant's folder)                                                   |
| --cognitiveModelsFile [path]  | (OPTIONAL) Path to your Cognitive Models file (defaults to 'cognitivemodels.json' inside your assistant's folder)                                           |
| --verbose                     | (OPTIONAL) Output detailed information about the processing of the tool                                                                                     |
| -h, --help                    | Output usage information                                                                                                                                    |

An example on how to use it with a local Skill manifest file:

```bash
botskills connect --localManifest "./skills/customSkill/customSkillManifest.json" --assistantSkills "./skills.json" --verbose 
```

> **Note:** The paths to both the Skill Manifest and the assistant Skills configuration file can be relative or absolute paths equally, and should be explicitly a `.json` file.

An example on how to use it with a remote Skill manifest:

```bash
botskills connect --remoteManifest "http://<YOUR_SKILL_MANIFEST>.azurewebsites.net/api/skill/manifest?inlineTriggerUtterances=false" --assistantSkills "./skills.json" --verbose 
```

# Disconnect a Skill from your assistant

To disconnect a Skill from your assistant:

```bash
botskills disconnect [option]
```

### Options

| Option                   | Description                                                             |
|--------------------------|-------------------------------------------------------------------------|
| -s, --skillName <path>   | Name or id of the skill to remove from your assistant (case sensitive)  |
| -a, --skillsFile <path>  | Path to the assistant Skills configuration file                         |
| --verbose                | (OPTIONAL) Output detailed information about the processing of the tool |
| -h, --help               | Output usage information                                                |

An example on how to use it:

```bash
botskills disconnect --skillName "customSkill" --assistantSkills "./skills.json" --verbose
```

> **Note:** The path to the assistant Skills configuration file can be relative or absolute path, and should be explicitly a `.json` file.