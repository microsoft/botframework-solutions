---
category: Skills
subcategory: Samples
language: experimental_skills
title: Phone Skill
description: Phone Skill provides the ability to find and call a person or a number.
order: 9
toc: true
---

# {{ page.title }}
{:.no_toc}

The Phone Skill provides the capability to start phone calls to a Virtual Assistant.

## Supported scenarios

The following scenarios are currently supported by the Skill:

- Outgoing Call
  - *Call Sanjay Narthwani*
  - *Call 555 5555*
  - *Make a call*

The skill will automatically prompt the user for any missing information and/or to clarify ambiguous information.

### Example dialog
{:.no_toc}

Here is an example of a dialog with the Phone skill that showcases all possible prompts.
Note that the skill may skip prompts if the corresponding information is already given.
This example assumes that the user's contact list contains multiple contacts named "Sanjay", one of which is named "Sanjay Narthwani" and has multiple phone numbers, one of which is labelled "Mobile".

|Turn| Utterance/ Prompt |
|-|-|
|User| Make a call |
|Skill| Who would you like to call? |
|User| Sanjay |
|Skill| Which Sanjay? |
|User| Narthwani |
|Skill| Sanjay Narthwani has multiple phone numbers. Which one? |
|User| Mobile |
|Skill| Calling Sanjay Narthwani on mobile. |

Refer to the unit tests for further example dialogs.

## Language Understanding

LUIS models for the Skill are provided in `.lu` file format as part of the Skill. Further languages are being prioritized.

|Supported Languages|
|-|
|English|

The LUIS model `phone` is used to understand the user's initial query as well as responses to the prompt "Who would you like to call?"
The other LUIS models (`contactSelection` and `phoneNumberSelection`) are used to understand the user's responses to later prompts in the dialog.

### Intents
{:.no_toc}

|Name|Description|
|-|-|
|OutgoingCall| Matches queries to make a phone call |

### Entities
{:.no_toc}

|Name|Description|
|-|-|
|contactName| The name of the contact to call |
|phoneNumber| A literal phone number specified by the user in the query, in digits |
|phoneNumberSpelledOut| A literal phone number specified by the user in the query, in words |
|phoneNumberType| Identifies a certain phone number of the contact by its type (for example, "home", "business", "mobile") |

## Deployment
{:.no_toc}
Learn how to [provision your Azure resources]({{site.baseurl}}/skills/tutorials/create-skill/csharp/4-provision-your-azure-resources/) in the Create a Skill tutorial.

## Configuration 

### Supported content providers
{:.no_toc}

> Office 365 and Outlook.com through the Microsoft Graph is supported along with support for Google accounts.

#### Authentication connection settings
{:.no_toc}

##### Office 365

This skill uses the following authentication scopes:

- `User.ReadBasic.All`
- `User.Read`
- `People.Read`
- `Contacts.Read`

You must use [these steps]({{site.baseurl}}/{{site.data.urls.SkillManualAuth}}) to manually configure Authentication for the Calendar Skill. Due to a change in the Skill architecture this is not currently automated.

> Ensure you configure all of the scopes detailed above.

#### Google Account

To use Google account in skill you need to follow these steps:
1. Create your Gmail API credential in [Google developers console](https://console.developers.google.com).
2. Create an OAuth connection setting in your Web App Bot.
    - Connection name: `googleapi`
    - Service Provider: `Google`
    - Client id and secret are generated in step 1
    - Scopes: `"https://www.googleapis.com/auth/contacts"`.
3. Add the connection name, client id, secret and scopes in appsetting.json file.

## Events

Note that the Phone skill only handles the dialog with the user about the phone call to be made, but does not place the actual phone call.
The phone call would typically be placed by the client application communicating with the bot or skill.
For example, if the client application is an Android app, it would communicate with the bot to allow the user to go through the dialog and at the end, it would place the call using an Android mechanism for placing calls.

The information that is required to place the call is returned from the Phone skill in the form of an event at the end of the dialog.
This event has the name `PhoneSkill.OutgoingCall`.
Its value is a JSON object representing an object of type `PhoneSkill.Models.OutgoingCall`.

The value of the event has the following properties:
- The property `Number` holds the phone number to be dialed as a string.
  (Please note that this string is in the same format as it appears in the user's contact list or in the user's query.
  If you require an RFC 3966 compliant `tel:` URI or a particular other format, we recommend using a phone number formatting library to format this string accordingly, taking into account the user's default country code and any other relevant external information.)
- The property `Contact` is optional and holds the contact list entry that the user selected.
  This is an object of type `PhoneSkill.Models.ContactCandidate`.
  This information may be useful, for example, to allow the client application to show information about the contact on the screen while the phone number is being dialed.

Here is an example of an event returned by the Phone skill:

```
{
    [...]
    "type": "event",
    "name": "PhoneSkill.OutgoingCall",
    "value": {
    "Number": "555 111 1111",
    "Contact": {
        "CorrespondingId": "[...]",
        "Name": "Andrew Smith",
        "PhoneNumbers": [
        {
            "Number": "555 111 1111",
            "Type": {
            "FreeForm": "",
            "Standardized": 1
            }
        }
        ]
    }
    }
}
```