# Creating a Skill

## Getting Started

An initial Skill template has been made available to simplify creation of your own skill. This can be found within the [Skill-Template](https://github.com/Microsoft/AI/tree/master/templates/Skill-Template) folder of the repository.

Create a new folder called `Bot Framework` within your `%userprofile%\Documents\Visual Studio 2017\Templates\ProjectTemplates\Visual C#' folder. Then within this create a` Virtual Assistant Skill` folder and copy the contents of the template into this folder.

Restart Visual Studio, create a new project and you should now skill the `Skill Template` appear within the available C#\Bot Framework Templates.

## Adding your Skill to your Virtual Assistant

- Add a project reference to your new Skill project ensuring that the Virtual Assistant can locate your assemblies when invoking the skill
- Add the LUIS model corresponding to your new Skill to the Virtual Assistant bot file through the following command
```shell
msbot connect luis --appId [LUIS_APP_ID] --authoringKey [LUIS_AUTHORING_KEY] --subscriptionKey [LUIS_SUBSCRIPTION_KEY]
```
- Run the following command to update the Dispatcher model to reflect the new dispatch target
```shell
dispatch refresh --bot "YOURBOT.bot" --secret YOURSECRET
```
- Generate an updated Dispatch model for your Assistant to enable evaluation of incoming messages. The Dispatch.cs folder is located in the `assistant\Dialogs\Shared` folder. Ensure you run this command within the assistant directory of your cloned repo.
```shell
msbot get dispatch --bot "YOURBOT.bot" | luis export version --stdin | luisgen - -cs Dispatch -o Dialogs\Shared
```
- Update the `assistant\Dialogs\Main\MainDialog.cs` file to include the corresponding Dispatch intent for your skill to the Skill handler, excerpt is shown below. add Authentication providers and configuration information as required.
```
   case Dispatch.Intent.l_Calendar:
   case Dispatch.Intent.l_Email:
   case Dispatch.Intent.l_ToDo:
   case Dispatch.Intent.l_PointOfInterest:
   {}
````
- Finally add the your Skill configuration to the appSettings.json file
```
 {
      "type": "skill",
      "id": "YOUR_SKILL_NAME",
      "name": "YOUR_SKILL_NAME",
      "assembly": "YourSkillNameSpace.YourSkillClass, YourSkillAssembly, Version=1.0.0.0, Culture=neutral",
      "dispatchIntent": "l_YOURSKILLDISPATCHINTENT",
      "supportedProviders": [
      ],
      "luisServiceIds": [
        "YOUR_SKILL_LUIS_MODEL_NAME",
        "general"
      ],
      "parameters": [
        "IPA.Timezone"
      ],
      "configuration": {
      }
    }
```