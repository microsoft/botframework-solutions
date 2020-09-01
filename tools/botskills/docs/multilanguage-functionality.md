# Multilanguage functionality
We introduced the possibility to connect multiple languages of the same Skill at the same time to the Virtual Assistant using Botskills CLI Tool.

Connections depend on the triangulation of the following language sources:
* The `Dispatch models` available in the Virtual Assistant 
* The `languages for the intents` in the [Skill Manifest](https://microsoft.github.io/botframework-solutions/skills/handbook/manifest/)
* The `--languages` argument of the `botskills connect` command

The only way to successfully execute the connection of several languages between a Virtual Assistant and a Skill would be if the `--languages` argument contains the same or less values than the result of the intersection between the Dispatch models languages and the Skill manifest intents languages. 

## Known scenarios
Assuming this scenario:
* Dispatch models in `en-us`, `es-es`, `fr-fr` available in the Virtual Assistant
* The Skill Manifest contains `en-us`, `es-es`, `it-it` as languages for the intents

In this case, the intersection between the `Dispatch models` and the `languages for the intents` is `en-us` and `es-es`. So, the `--languages` argument should contain:
* `en-us,es-es`
* `en-us`
* `es-es`

> If the tool identifies an invalid language, it will stop the execution without connecting the valid languages.

## Examples
Taking into account the scenario mentioned, we will connect the `en-us` and `es-es` languages of the Skill to the Virtual Assistant

```bash
botskills connect --remoteManifest "http://<YOUR_SKILL_NAME>.azurewebsites.net/manifest/manifest-1.1.json" --cs --languages "en-us,es-es" --luisFolder "<PATH_TO_LU_FOLDER>"
```

> Since `--languages` is an optional argument, it will connect `en-us` by default, unless we pass a value to the argument.

### Further Reading
- [Connect Command](./commands/connect.md)
- [Language and region support for LUIS](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-language-support)