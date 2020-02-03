---
category: Skills
subcategory: Samples
language: experimental_skills
title: Automotive Skill
description: Automotive Skill provides the ability to issue comands to vehicles to control settings in a vehicle.
order: 2
toc: true
---

# {{ page.title }}
{:.no_toc}


The Automotive Skill is in preview and demonstrates the first capabilities to help enable Automotive scenarios. The skill focuses on Vehicle Settings, specifically Climate Control, Safety and Basic audio controls. Media, Tuner and Phone capabilities are expected in a future release.

Vehicle Control is a complicated domain, whilst there are only a limited set of car controls for climate control there are a myriad of ways that a human can describe a given setting. For example, *I'm feeling chilly* , *My feet are cold* and *It's cold here in the back* all relate to a decrease in temperature but to different parts of the car and perhaps even different fan settings.

The Skill leverages a set of LUIS models to help understand the intent and entities but then leverages capabilities from our Maluuba team to match potential settings and actions to the available settings to then suggest a course of action.

Unlike the Productivity and PoI skills that are integrated into existing services, the automotive skill will require integration with the telematics solution in use by a given OEM so will require customization to reflect actual car features for a given OEM along with integration.

To enable testing and simulation any action identified is surfaced to the calling application as an event, this can easily be seen within the Bot Framework Emulator and will be wired up into the Web Test harness available as part of the Virtual Assistant solution.

## Supported scenarios

At this time, changes to vehicle settings are supported through the `VEHICLE_SETTINGS_CHANGE` and `VEHICLE_SETTINGS_DECLARATIVE` intents. The former enables questions such as "change the temperature to 21 degrees" whereas the latter intent enables scenarios such as "I'm feeling cold" which require additional processing steps.

The following vehicle setting areas are supported at this time, example utterances are provided for guidance. In cases where the utterance results in multiple potential settings or a value isn't provided then the skill will prompt for disambiguation. Confirmation will be sought from the user if a setting is configured to require confirmation, important for sensitive settings such as safety.

### Climate Control
{:.no_toc}

- *Set temperature to 21 degrees*
- *Defog my windshield*
- *Put the air on my feet*
- *Turn off the ac*
- *I'm feeling cold*
- *It's feeling cold in the back*
- *The passenger is freezing*
- *Change climate control*

### Safety
{:.no_toc}

- *Turn lane assist off*
- *Enable lane change alert*
- *Set park assist to alert*

### Audio
{:.no_toc}

- *Adjust the equalizer*
- *Increase the bass*
- *Increase the volume*

Vehicle settings can be selected through explicit entry of the vehicle setting name, numeric or ordinal (first one, last one).

An example transcript file demonstrating the Skill in action can be found [here]({{site.baseurl}}/assets/transcripts/skills-automotive.transcript), you can use the Bot Framework Emulator to open transcripts.

![ Automotive Skill Transcript Example]({{site.baseurl}}/assets/images/skills-auto-transcript.png)

## Language Understanding

LUIS models for the Skill are provided in `.lu` file format as part of the Skill. These are currently available in English with other languages to follow.

The following Top Level intents are available with the main `settings` LUIS model

- VEHICLE_SETTINGS_CHANGE
- VEHICLE_SETTINGS_DECLARATIVE

In addition there are two supporting LUIS models `settings_name` and `settings_value`, these are used for disambiguation scenarios to clarify setting names and values where the initial utterance doesn't provide clear information.

## Configuration


### Customizing vehicle settings
{:.no_toc}

Available vehicle settings are defined in a supporting metadata file which you can find in this location:  `automotiveskill/Dialogs/VehicleSettings/Resources/available_settings.yaml`.

To add an new setting along with appropriate setting values it's easily expressed in YAML. The example below shows a new Volume control setting with the ability to Set, Increase, Decrease and Mute the volume.

```
canonicalName: Volume
values:
  - canonicalName: Set
    requiresAmount: true
  - canonicalName: Decrease
    changesSignOfAmount: true
  - canonicalName: Increase
    antonym: Decrease
  - canonicalName: Mute
allowsAmount: true
amounts:
  - unit: ''
```

 For key settings you may wish to prompt for confirmation, safety settings for example. This can be specified through a `requiresConfirmation` property as shown below.

```
canonicalName: Lane Change Alert
values:
  - canonicalName: Off
    requiresConfirmation: true
  - canonicalName: On
```

### Deployment
{:.no_toc}
Learn how to [provision your Azure resources]({{site.baseurl}}/skills/tutorials/create-skill/csharp/4-provision-your-azure-resources/) in the Create a Skill tutorial.

## Events

The Automotive Skill surfaces setting changes for testing purposes through an event returned to the client. This enables easy testing and simulation, all events are prefixed with `AutomotiveSkill.`. The below event is generated as a response to `I'm feeling cold`

```json
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