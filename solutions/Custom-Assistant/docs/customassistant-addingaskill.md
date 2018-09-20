---
title: Adding Skills to your Custom Assistant | Microsoft Docs
description: Learn about how to add skills to your Custom Assistant
author: darrenj
ms.author: darrenj
manager: kamrani
ms.topic: article
ms.prod: bot-framework
ms.date: 13/12/2018
monikerRange: 'azure-bot-service-3.0'
---
# Adding Skills to your Custom Assistant 

## Overview

## Authentication Connection Name

If the Skill you wish to add requires User Tokens in order to complete a task you will need to add a new Authentication Connection to your Bot through the Settings section of the Azure Bot Service blade in the Azure portal. See the [Auth Connection Settings](./customassistant-skills.md) section of the Skill documentation to ensure this is configured correctly.

## Skill Configuration

The first step is to add a Skill Registration entry to your Custom Assistant `appsettings.json` file. This is used by the Custom Assistant to understand what skills are available and how to map a given question to a Skill.

See the [Skills](./customassistant-skills.md) section for a configuration file example for each of the available skills. Modify this to suit your scenario and add a new element under the Skills element, an example is shown below. Ensure you use the Authentication Connection Name created in the previous step.

```
"Skills": [  
   {
    "Name": "Calendar",
    "DispatcherModelName": "l_IPABot_Calendar",
    "Description": "The Calendar Skill adds Email related capabilities to your Custom Assitant",
    "Assembly": "CalendarSkill.CalendarSkill, CalendarSkill, Version=1.0.0.0, Culture=neutral",
    "AuthConnectionName": "YOUR_AUTH_CONNECTION_NAME",
    "Parameters": [
    "IPA.Timezone"
    ],
    "Configuration": {
    "LuisAppId": "YOUR_LUIS_APP_ID",
    "LuisSubscriptionKey": "YOUR_LUIS_SUBSCRIPTION_KEY",
    "LuisEndpoint": "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/"
    }
}
```

## Dispatch Refresh

> TODO

## Add Handler to your MainDialog

> This code change will be removed in a future release through an update to the Dispatch CLI to tag dispatch targets as Skills

Within the `MainDialog.cs` file the Dispatch intents are evaluated, you'll need to add a new handler for your new Skill.

```
case Dispatch.YOUR_DISPATCH_SKILL:
{
    var userInformation = await _ipaAccessors.UserInformation.GetAsync(dc.Context, () => new System.Collections.Generic.Dictionary<string, object>());

    var luisService = _services.LuisServices["YOUR_SKILL_LUISMODEL_NAME"];
    var luisResult = await luisService.RecognizeAsync<Calendar>(dc.Context, CancellationToken.None);
    var matchedSkill = _skillRouter.IdentifyRegisteredSkill(intent.ToString());

    await RouteToSkill(dc, new SkillDialogOptions()
    {
        LuisResult = luisResult,
        MatchedSkill = matchedSkill,
        UserInfo = userInformation,
    });

    break;
}
```
## Test your Skill

Your Skill is now ready for use, follow the [Test](./customassistant-testing.md) instructions to get started.