# Virtual Assistant Skills - Point of Interest

## Overview
The Point of Interest Skill provides PoI related capabilities to a Virtual Assistant. The most common scenarios have been implemented in this first release with additional scenarios in development.

## Supported Scenarios
The following scenarios are currently supported by the Skill:

- NAVIGATION_ROUTE_FROM_X_TO_Y
    - What's the fastest way to get to 221B Baker Street?
    - How do I get to the grocery store?
    - I need directions to a cafe
- NAVIGATION_FIND_POINTOFINTEREST
    - What's nearby?
    - Are there any pharmacies in town?
    - Can you recommend an affordable restaurant in Seattle?
- NAVIGATION_CANCEL_ROUTE
    - I don't want to go to the shop anymore
    - Would you cancel my route?
    - On second thought, forget going to the airport

## Supported Sources

> [Azure Maps](https://azure.microsoft.com/en-gb/services/azure-maps/) is supported at this time. A provider model is planned for a coming release to enable plugin of your own data source.

## Auth Connection Settings

> No Authentication is required for this skill

## Skill Parameters
The following Parameters are accepted by the Skill and enable additional personalisation of responses to a given user:
- IPA.Location (*The skill will fail without this as it is missing a user's current coordinates*)

## Configuration File Information
The following Configuration entries are required to be passed to the Skill and are provided through the Virtual Assistant appSettings.json file. These should be updated to reflect your LUIS deployment.

- LuisAppId
- LuisSubscriptionKey
- LuisEndpoint
- AzureMapsKey.

> You can retrieve an [Azure Maps](https://azure.microsoft.com/en-gb/services/azure-maps/) key through the Azure Portal.

## Example Skill Registration Entry
```
{
    "Name": "PointOfInterest",
    "DispatcherModelName": "l_PointOfInterest",
    "Description": "The Point of Interest Skill adds PoI related capabilities to your Custom Assitant",
    "Assembly": "PointOfInterestSkill.PointOfInterestSkill, PointOfInterestSkill, Version=1.0.0.0, Culture=neutral",
    "AuthConnectionName": "",
    "Parameters": [
    "IPA.Timezone"
    ],
    "Configuration": {
    "LuisAppId": "YOUR_LUIS_APP_ID",
    "LuisSubscriptionKey": "YOUR_LUIS_SUBSCRIPTION_KEY",
    "LuisEndpoint": "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/"
    "AzureMapsKey": "YOUR_AZURE_MAPS_KEY"
    }
}
```

## LUIS Model Intents and Entities
LUIS models for the Skill are provided in .LU file format as part of the Skill. These are currently available in English, French, Italian, German and Spanish languages. Further languages are being prioritised.

The following Top Level intents are available:


- NAVIGATION_ROUTE_FROM_X_TO_Y
- NAVIGATION_FIND_POINTOFINTEREST
- NAVIGATION_CANCEL_ROUTE

The following entities are provided:

- ADDRESS
- KEYWORDS
- DESCRIPTOR

## Event Responses

The Point of Interest Skill surfaces a users request to navigate to a new destination through an event returned to the client. The event is called `ActiveRoute.Directions" has contains a series of Points for the Route along with a summary of the route information. A simplified example is shown below

```
{
  "name": "ActiveRoute.Directions",
  "type": "event",
  "value": [
    {
      "points": [
        {
          "latitude": 47.64056,
          "longitude": -122.129372
        },
        {
          "latitude": 47.64053,
          "longitude": -122.129387
        },
    
        ...

      ],
      "summary": {
        "arrivalTime": "2018-09-18T04:17:25Z",
        "departureTime": "2018-09-18T03:54:35Z",
        "lengthInMeters": 20742,
        "trafficDelayInSeconds": 0,
        "travelTimeInSeconds": 1370
      }
    }
  ]
}
```
