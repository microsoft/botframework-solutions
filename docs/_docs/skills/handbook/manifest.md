---
category: Skills
subcategory: Handbook
title: Manifest
description: Overview of the Skill manifest and its role with Skill registration and invocation.
order: 2
toc: true
---

# {{ page.title }}
{:.no_toc}

The Skill manifest enables Skills to be self-describing in that they communicate the name and description of a Skill, it's authentication requirements if appropriate along with the discrete actions that it exposes. Each action provides utterances that the caller can use to identify when an utterance should be passed across to a skill along with slots (parameters) that it can accept for slot-filling if required.

This manifest provides all of the metadata required for a calling Bot to know when to trigger invoking a skill and what actions it provides. The manifest is used by the Skill command-line tool to configure a Bot to make use of a Skill.

Each skill exposes a manifest endpoint enabling easy retrieval of a manifest, this can be found on the following URI path of your skill: **/api/skill/manifest**

## Manifest structure

A manifest is made up of the following structure:

- Manifest Header
- Authentication Connections
- Actions
  - Definition
    - Slot
  - Trigger
    - UtteranceSources
    - Utterance

### Manifest Header
{:.no_toc}

The manifest header provides high level information relating to your skill, the table below provides more information on each item. Note that items marked as automatic should not be provided in your manifest file as they are automatically provided at runtime as part of the manifest generation.

 Parameter  | Description | Required
 ---------  | ----------- | --------
 id         | Identifier for your skill, no spaces or special characters | **Yes**
 name       | Display name for your skill | **Yes**
 description| Description of the capabilities your Skill provides | **Yes**
 iconUrl    | Icon Uri representing your skill, potentially used to show the skills registered with a Bot. | No
 msaAppId   | Microsoft App Id value of your skill | Automatic
 endpoint   | Exposed entrypoint for communicating with your skill | Automatic

```json
  "id": "calendarSkill",
  "name": "Calendar Skill",
  "description": "The Calendar skill provides calendaring related capabilities and supports Office and Google calendars.",
  "iconUrl": "calendarSkill.png",
  "msaAppId": "10101010-1010-1010-1010-101010101010",
  "endpoint": "http://localhost:3980/api/skill/messages",
```

### Authentication Connections
{:.no_toc}

The **authenticationConnections** section communicates which authentication providers your skill supports, if any. For example, a Calendar skill might support both Outlook and Google enabling it to function with either provider depending on the users choice. The caller can then use this information to automatically configure the Authentication connection or as required enable a manual step to be performed.

 Parameter  | Description | Required
 ---------  | ----------- | --------
 id                 | Identifier for the authentication connection, no spaces or special characters | **Yes**
 serverProviderId   | The Service Provider identifier to help the client understand which identity provider it should use | **Yes**
 scopes             | The authentication scopes required for this skill to operate. Space or comma separated | **Yes**

```json
"authenticationConnections": [
  {
    "id": "Outlook",
    "serviceProviderId": "Azure Active Directory v2",
    "scopes": "User.ReadBasic.All, Calendars.ReadWrite, People.Read, Contacts.Read"
  },
  {
    "id": "Google",
    "serviceProviderId": "Google",
    "scopes": "https://www.googleapis.com/auth/calendar https://www.googleapis.com/auth/contacts"
  }
]
```

### Actions
{:.no_toc}

The **actions** section describes the discrete actions (features) that a given Skill supports. Each action can optionally provide slots (parameters) that the caller may choose to pass or alternatively omit and pass the utterance for the Skill to perform it's own slot filling. Slot filling on the client side can enable a Skill to be invoked and not require any further input or turns from the end user.

Parameter  | Description | Required
 ---------  | ----------- | --------
 id                     | Identifier for the action. No spaces or special characters | **Yes**
 definition/description | Description of what the action provides | **Yes**
 definition/slots       | A name/types collection of each slot | **Yes**

```json
"actions": [
  {
    "id": "calendarskill_createEvent",
    "definition": {
      "description": "Create a new event",
      "slots": [
        {
          "name": "title",
          "types": [
            "string"
          ]
        },
        {
          "name": "content",
          "types": [
            "string"
          ]
        }
      ]
    }
  }
]
```

### Trigger
{:.no_toc}

A given action can be trigged through different mechanisms or an utterance. Example triggering utterances must be provided by a skill to enable a caller to train a natural language dispatcher so it can identify utterances that should be routed to a skill.

References to an source of utterances can be provided through the (`utteranceSource`) element.

> At this time we only support a LU file reference which the Skill CLI parses and resolves the LU file locally meaning the developer must have the LU file available. Moving forward we plan to add a reference to a deployed LUIS model meaning this can be retrieved dynamically.

```json
"triggers": {
  "utteranceSources": [
    {
      "locale": "en",
      "source": [
        "Calendar#AcceptEventEntry",
        "Calendar#DeleteCalendarEntry"
      ]
    }
  ]
}
```

Utterances can also be provided in-line with the skill manifest as shown below. Unlike with `utteranceSource` all utterances are provided as part of the manifest providing the Skill CLI everything it needs for trigger utterances.

```json
"triggers": {
  "utterances": [
    {
      "locale": "en",
      "text": [
        "2 hour meeting with darren at 5 on tuesday",
        "add a meeting with darren to my calendar"
      ]
    }
  ]
}
```

Both **utteranceSources** and **utterances** support multiple-locales enabling you to express the locales your Skill supports.

### Example Skill Manifest
{:.no_toc}

```json
{
  "id": "calendarSkill",
  "name": "Calendar Skill",
  "description": "The Calendar skill provides calendaring related capabilities and supports Office and Google calendars.",
  "iconUrl": "calendarSkill.png",
  "authenticationConnections": [
    {
      "id": "Outlook",
      "serviceProviderId": "Azure Active Directory v2",
      "scopes": "User.ReadBasic.All, Calendars.ReadWrite, People.Read, Contacts.Read"
    },
    {
      "id": "Google",
      "serviceProviderId": "Google",
      "scopes": "https://www.googleapis.com/auth/calendar https://www.googleapis.com/auth/contacts"
    }
  ],
  "actions": [
    {
      "id": "calendarskill_createEvent",
      "definition": {
        "description": "Create a new event",
        "slots": [
          {
            "name": "title",
            "types": [
              "string"
            ]
          },
          {
            "name": "content",
            "types": [
              "string"
            ]
          },
          {
            "name": "attendees",
            "types": [
              "string"
            ]
          },
          {
            "name": "startDate",
            "types": [
              "string"
            ]
          },
          {
            "name": "startTime",
            "types": [
              "string"
            ]
          },
          {
            "name": "duration",
            "types": [
              "string"
            ]
          },
          {
            "name": "location",
            "types": [
              "string"
            ]
          }
        ],
        "triggers": {
          "utteranceSources": [
            {
              "locale": "en",
              "source": [
                "Calendar#CreateCalendarEntry",
                "Calendar#FindMeetingRoom"
              ]
            }
          ]
        }
      }
    },
    {
      "id": "calendarskill_changeEventStatus",
      "definition": {
        "description": "Change the status of an event (accept/decline).",
        "slots": [
          {
            "name": "startDate",
            "types": [
              "string"
            ]
          },
          {
            "name": "startTime",
            "types": [
              "string"
            ]
          }
        ],
        "triggers": {
          "utteranceSources": [
            {
              "locale": "en",
              "source": [
                "Calendar#AcceptEventEntry",
                "Calendar#DeleteCalendarEntry"
              ]
            }
          ]
        }
      }
    },
    {
      "id": "calendarskill_joinEvent",
      "definition": {
        "description": "Join the upcoming meeting",
        "slots": [],
        "triggers": {
          "utteranceSources": [
            {
              "locale": "en",
              "source": [
                "Calendar#ConnectToMeeting"
              ]
            }
          ]
        }
      }
    },
    {
      "id": "calendarskill_timeRemaining",
      "definition": {
        "description": "Find out how long until the next event",
        "slots": [],
        "triggers": {
          "utteranceSources": [
            {
              "locale": "en",
              "source": [
                "Calendar#TimeRemaining"
              ]
            }
          ]
        }
      }
    },
    {
      "id": "calendarskill_summary",
      "definition": {
        "description": "Find an upcoming event",
        "slots": [
          {
            "name": "startDate",
            "types": [
              "string"
            ]
          },
          {
            "name": "startTime",
            "types": [
              "string"
            ]
          },
          {
            "name": "endDate",
            "types": [
              "string"
            ]
          },
          {
            "name": "endTime",
            "types": [
              "string"
            ]
          }
        ],
        "triggers": {
          "utteranceSources": [
            {
              "locale": "en",
              "source": [
                "Calendar#FindCalendarDetail",
                "Calendar#FindCalendarEntry",
                "Calendar#FindCalendarWhen",
                "Calendar#FindCalendarWhere",
                "Calendar#FindCalendarWho",
                "Calendar#FindDuration"
              ]
            }
          ]
        }
      }
    },
    {
      "id": "calendarskill_updateEvent",
      "definition": {
        "description": "Update an existing event.",
        "slots": [
          {
            "name": "startDate",
            "types": [
              "string"
            ]
          },
          {
            "name": "startTime",
            "types": [
              "string"
            ]
          },
          {
            "name": "endDate",
            "types": [
              "string"
            ]
          },
          {
            "name": "endTime",
            "types": [
              "string"
            ]
          },
          {
            "name": "newStartDate",
            "types": [
              "string"
            ]
          },
          {
            "name": "newStartTime",
            "types": [
              "string"
            ]
          }
        ],
        "triggers": {
          "utteranceSources": [
            {
              "locale": "en",
              "source": [
                "Calendar#ChangeCalendarEntry"
              ]
            }
          ]
        }
      }
    }
  ]
}
```
