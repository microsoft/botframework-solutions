# Experimental Skills

## Overview

Experimental Skills are early prototypes of Skills to help bring skill concepts to life for demonstrations and proof-of-concepts along with providing different examples to get you started.

These skills by their very nature are not complete, will likely have rudimentary language models, limited language support and limited testing hence are located in a experimental folder to ensure this is understood before you make use of them.

## Restaurant Booking Skill

The [Restaurant Booking](../src/csharp/experimental/skills/restaurantbooking/readme.md) skill provides a simple restaurant booking experience guiding the user through booking a table and leverages Adaptive Cards throughout to demonstrate how Speech, Text and UX can be combined for a compelling user experience. No integration to restaurant booking services exists at this time so is simulated with static data for testing purposes.

## News Skill


## Deploying Experimental Skills in local-mode

Experimental Skills not added by default when deploying the Virtual Assistant due to their experimental nature.

Run this PowerShell script to deploy shared resources and LUIS models required for an experimental skill.

```
  PowerShell.exe -ExecutionPolicy Bypass -File DeploymentScripts\deploy_bot.ps1
```

You will be prompted to provide the following parameters:
   - Name - A name for your bot and resource group. This must be **unique**.
   - Location - The Azure region for your services (e.g. westus)
   - LUIS Authoring Key - Refer to [this documentation page](./virtualassistant-createvirtualassitant.md) for retrieving this key.

The msbot tool will outline the deployment plan including location and SKU. Ensure you review before proceeding.

> After deployment is complete, it's **imperative** that you make a note of the .bot file secret provided as this will be required for later steps. The secret can be found near the top of the execution output and will be in purple text.

- Update your `appsettings.json` file with the newly created .bot file name and .bot file secret.
- Run the following command and retrieve the InstrumentationKey for your Application Insights instance and update `InstrumentationKey` in your `appsettings.json` file.

```
msbot list --bot YOURBOTFILE.bot --secret YOUR_BOT_SECRET
```

```
  {
    "botFilePath": ".\\YOURBOTFILE.bot",
    "botFileSecret": "YOUR_BOT_SECRET",
    "ApplicationInsights": {
      "InstrumentationKey": "YOUR_INSTRUMENTATION_KEY"
    }
  }
```

- Finally, add the .bot file paths for each of your language configurations (English only at this time).

```
"defaultLocale": "en-us",
  "languageModels": {
    "en": {
      "botFilePath": ".\\LocaleConfigurations\\YOUR_EN_BOT_PATH.bot",
      "botFileSecret": ""
    }
    }
```
## Testing the skill in local-mode

Once you have followed the deployment instructions above, open the provided .bot file with the Bot Framework Emulator.
