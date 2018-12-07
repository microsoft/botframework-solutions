# Virtual Assistant Skills - Automotive (Preview)

## Overview
The Automotive Skill is in preview and demonstrates the first capabilities to help enable Automotive scenarios. The skill focuses on Vehicle Settings, specifically Climate Control, Safety and Basic audio controls. Media, Tuner and Phone capabilities are expected in a future release.

Vehicle Control is a complicated domain, whilst there are only a limited set of car controls for climate control there are a myriad of ways that a human can describe a given setting. For example, `I'm feeling chilly` , `My feet are cold` and `It's cold here in the back` all relate to a decrease in temperature but to different parts of the car and perhaps even different fan settings.

The Skill leverages a set of LUIS models to help understand the intent and entities but then leverages capabilities from our Maluuba team to match potential settings and actions to the available settings to then suggest a course of action.

Unlike the Productivity and PoI skills that are integrated into existing services, the automotive skill will require integration with the telematics solution in use by a given OEM so will require customisation to reflect actual car features for a given OEM along with integration.

To enable testing and simulation any action identified is surfaced to the calling application as an event, this can easily be seen within the Bot Framework Emulator and will be wired up into the Web Test harness available as part of the Virtual Assistant solution.

## Supported Scenarios

At this time, changes to vehicle settings are supported through the `VEHICLE_SETTINGS_CHANGE` and `VEHICLE_SETTINGS_DECLARATIVE` intents. The former enables questions such as "change the temperature to 21 degrees" whereas the latter intent enables scenarios such as "I'm feeling cold" which require additional processing steps.

The following vehicle setting areas are supported at this time, example utterances are provided for guidance. In cases where the utterance results in multiple potential settings or a value isn't provided then the skill will prompt for disambiguation. Confirmation will be sought from the user if a setting is configured to require confirmation, important for sensitive settings such as safety.

### Climate Control

- *Set temperature to 21 degrees*
- *Defog my windshield*
- *Put the air on my feet*
- *Turn off the ac*
- *I'm feeling cold*
- *It's feeling cold in the back*
- *The passenger is freezing*
- *Change climate control*

### Safety

- *Turn lane assist off*
- *Enable lane change alert*
- *Set park assist to alert*

### Audio

- *Adjust the equalizer*
- *Increase the bass*
- *Increase the volume*

## Authentication Connection Settings

> No Authentication is required for this skill

## Skill Parameters

> No Parameters are required for this skill

## Example Skill Registration Entry
```
{
    "type": "skill",
    "id": "automotiveSkill",
    "name": "automotiveSkill",
    "assembly": "AutomotiveSkill.AutomotiveSkill, AutomotiveSkill, Version=1.0.0.0, Culture=neutral",
    "dispatchIntent": "l_Automotive",
    "supportedProviders": [],
    "luisServiceIds": [
      "settings",
      "settings_name",
      "settings_value",
      "general"
    ],
    "parameters": []
    ],
    "configuration": { }
}
```

## LUIS Model Intents and Entities
LUIS models for the Skill are provided in .LU file format as part of the Skill. These are currently available in English with other languages to follow.

The following Top Level intents are available with the main `settings` LUIS model

- VEHICLE_SETTINGS_CHANGE
- VEHICLE_SETTINGS_DECLARATIVE

In addition there are two supporting LUIS models `settings_name` and `settings_value`, these are used for disambiguation scenarios to clarify setting names and values where the initial utterance doesn't provide clear information.

## Customising Vehicle Settings

Available vehicle settings are defined in a supporting metadata file which you can find in this location:  `automotiveskill\Dialogs\VehicleSettings\Resources\available_settings.json`.

To add an new setting along with appropriate setting values it's easily expressed in JSON. The example below shows a new Volume control setting with the ability to Set, Increase, Decrease and Mute the volume.

```
  {
    "allowsAmount": true,
    "amounts": [ { "unit": "" } ],
    "canonicalName": "Set Volume",
    "categories": [ "Audio" ],
    "values": [
      {
        "canonicalName": "Set",
        "requiresAmount": true
      },
      {
        "changesSignOfAmount": true,
        "canonicalName": "Decrease"
      },
      {
        "canonicalName": "Increase",
        "antonym": "Decrease"
      },
      {
        "canonicalName": "Mute"
      }
    ]
  }
 ```

 For key settings you may wish to prompt for confirmation, safety settings for example. This can be specified through a `requiresConfirmation` property as shown below.

```
 {
    "canonicalName": "Lane Change Alert",
    "categories": [ "Active Safety" ],
    "values": [
      {
        "canonicalName": "Off",
        "requiresConfirmation": true
      },
      { "canonicalName": "On" }
    ]
  },
```

## Event Responses

The Automotive Skill surfaces setting changes for testing purposes through an event returned to the client. This enables easy testing and simulation, all events are prefixed with `AutomotiveSkill.`. The below event is generated as a response to `I'm feeling cold`
```
{
  "name": "AutomotiveSkill.Temperature",
  "type": "event",
  "value": [
    {
      "Key": "valueingform",
      "Value": "Increasing"
    },
    {
      "Key": "settingname",
      "Value": "Temperature"
    }
  ]
}
```
