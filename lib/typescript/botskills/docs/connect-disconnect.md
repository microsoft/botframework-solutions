# Connect a Skill to your assistant

To connect a Skill to your assistant:

```bash
botskills connect [options]
```

### Options

| Option                        | Description                                                             |
|-------------------------------|-------------------------------------------------------------------------|
| -l, --localResource <path>    | Path to local Skill Manifest file                                       |
| -r, --remoteResource <path>   | URL to remote Skill Manifest                                            |
| -a, --assistantSkills <path>  | Path to the assistant Skills configuration file                         |
| --verbose                     | (OPTIONAL) Output detailed information about the processing of the tool |
| -h, --help                    | Output usage information                                                |

An example on how to use it:

```bash
botskills connect --skillManifest "./skills/customSkill/customSkillManifest.json" --assistantSkills "./skills.json" --verbose 
```

> **Note:** The paths to both the Skill Manifest and the assistant Skills configuration file can be relative or absolute paths equally, and should be explicitly a `.json` file.

# Disconnect a Skill from your assistant

To disconnect a Skill from your assistant:

```bash
botskills disconnect [option]
```

### Options

| Option                        | Description                                                             |
|-------------------------------|-------------------------------------------------------------------------|
| -s, --skillName <path>        | Name of the skill to remove from your assistant (case sensitive)        |
| -a, --assistantSkills <path>  | Path to the assistant Skills configuration file                         |
| --verbose                     | (OPTIONAL) Output detailed information about the processing of the tool |
| -h, --help                    | Output usage information                                                |

An example on how to use it:

```bash
botskills disconnect --skillName "customSkill" --assistantSkills "./skills.json" --verbose
```

> **Note:** The path to the assistant Skills configuration file can be relative or absolute path, and should be explicitly a `.json` file.