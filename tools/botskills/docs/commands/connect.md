# Connect a Skill to your Virtual Assistant

The `connect` command allows you to connect a Skill, be it local or remote, to your Virtual Assistant bot. The Skill and Virtual Assistant can be in different coding languages without problem, this is, you can connect a Skill coded in C# into an Virtual Assistant coded in TypeScript, but be sure to specify your Virtual Assistants coding language using `--cs` or `--ts`.

> It's highly advisable to execute this command from the **root folder of your Virtual Assistant bot**, so if you are using the suggested folder structure from the Templates, you may omit most of the optional arguments, as they default to the expected values from the Template's folder structure.

The basic command to connect a Skill to your Virtual Assistant:

```bash
botskills connect [options]
```

## Skill Deployment
See the [Skills Overview](https://microsoft.github.io/botframework-solutions/overview/skills/) section for details on the Skills provided as part of the Virtual Assistant Solution Accelerator. Follow the deployment instructions required for each skill you wish to use and then return to this section to add these skills to your Virtual Assistant.

## Intent Filtering
You can specify which of the skill intents should be included when connecting it to the Virtual Assistant.

Botskills will check the Skill's manifest `dispatchModel` property and extract the list of intents, adding only these intents to the Virtual Assistant manifest.

If Botskills finds an intent whose name is an asterisk (`*`), it will override the filtering functionality, including all the intents disregarding any other one specified inside the `dispatchModel`.

> If the skill manifest includes a wildcard intent (`*`) along with other intents, a warning will be shown alerting that all of the skill intents will be included.

## Options

| Option | Description |
| - | - |
| -l, --localManifest [path] | Path to local Skill Manifest file |
| -r, --remoteManifest [url] | URL to remote Skill Manifest |
| --cs | Determine your Virtual Assistant project structure to be a csharp-like structure |
| --ts | Determine your Virtual Assistant project structure to be a TypeScript-like structure |
| --noRefresh [true\|FALSE] | (OPTIONAL) Determine whether the skills models connected to the Virtual Assistant are not going to be trained and published in an existing dispatch model (by default they are trained) |
| -e, --endpointName [name] | (OPTIONAL) Name of the endpoint to connect to your assistant (case sensitive) (default to using the first endpoint) |
| --languages [languages] | (OPTIONAL) Comma separated list of locales used for LUIS culture (defaults to `en-us`) |
| --luisFolder [path] | (OPTIONAL) Path to the folder containing your Skills' '.lu' files (defaults to './deployment/resources/skills/en-us' inside your Virtual Assistant folder) |
| --dispatchFolder [path] | (OPTIONAL) Path to the folder containing your Virtual Assistant's `.dispatch` file (defaults to `./deployment/resources/dispatch` inside your Virtual Assistant folder) |
| --outFolder [path] | (OPTIONAL) Path for any output file that may be generated (defaults to your Virtual Assistant's root folder) |
| --lgOutFolder [path] | (OPTIONAL) Path for the Luis Generate output (defaults to a 'service' folder inside your Virtual Assistant's folder) |
| --resourceGroup [name] | (OPTIONAL) Name of your Virtual Assistant's resource group in Azure (defaults to your Virtual Assistant's bot name) |
| --appSettingsFile [path] | (OPTIONAL) Path to your appsettings file where the skills are stored (defaults to `appsettings.json` inside your Virtual Assistant's folder) |
| --cognitiveModelsFile [path] | (OPTIONAL) Path to your Cognitive Models file (defaults to `cognitivemodels.json` inside your Virtual Assistant's folder) |
| --verbose | (OPTIONAL) Output detailed information about the processing of the tool |
| -h, --help | Output usage information |

Here is an example:

```bash
botskills connect --remoteManifest "https://<YOUR_SKILL_MANIFEST>.azurewebsites.net/manifest/manifest-1.1.json" --cs --luisFolder "<PATH_TO_LU_FOLDER>"
```

*Remember to re-publish your Assistant to Azure after you've added a Skill unless you plan on testing locally only*

Once the connect command finish successfully, you can see under the `botFrameworkSkills` property of your Virtual Assistant's appsettings.json file that the following structure was added with the information provided in the Skill manifest.

```json
    "botFrameworkSkills": {
        "id": "<SKILL_ID>",
        "appId": "<SKILL_APPID>",
        "skillEndpoint": "<SKILL_ENDPOINT>",
        "name": "<SKILL_NAME>",
        "description": "<SKILL_DESCRIPTION>"
    },
    "skillHostEndpoint": "<VA_SKILL_ENDPOINT>"
```