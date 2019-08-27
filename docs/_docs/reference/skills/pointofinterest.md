---
category: Reference
subcategory: Skills
title: Point Of Interest Skill
description: Find points of interest and directions. Powered by Azure Maps and FourSquare.
order: 11
---

# {{ page.title }}
{:.no_toc}

## In this reference
{:.no_toc}

* 
{:toc}

## Overview
The Point of Interest Skill provides point of interest and navigation related capabilities to a Virtual Assistant.
The most common scenarios have been implemented in this beta release, with additional scenarios in development.

## Supported Scenarios

The following scenarios are currently supported by the Skill:

- Get Directions to a Point of Interest
  - _What's the fastest way to get to 221B Baker Street?_
  - _How do I get to the grocery store?_
  - _I need directions to a café_
- Find a Parking Space
  - _Find parking near the doctor's office_
  - _Where's the nearest parking garage?_
  - _Parking lot by the airport_
- Find a Point of Interest
  - _What's nearby?_
  - _Are there any pharmacies in town?_
  - _Can you recommend an affordable restaurant in Seattle?_
- Cancel an Active Route
  - _I don't want to go to the shop anymore_
  - _Would you cancel my route?_
  - _On second thought, forget going to the airport_

## Language Model

LUIS models for the Skill are provided in .LU file format as part of the Skill. Further languages are being prioritized.

|Supported Languages|
|-|
|English|
|French|
|Italian|
|German|
|Spanish|
|Chinese (simplified)|

### Intents

|Name|Description|
|-|-|
|NAVIGATION_ROUTE_FROM_X_TO_Y| Matches queries navigating to a point of interest |
|NAVIGATION_FIND_POINTOFINTEREST| Matches queries searching for a point of interest |
|NAVIGATION_FIND_PARKING| Matches queries searching for a parking space |
|NAVIGATION_CANCEL_ROUTE| Matches queries to cancel a route |

### Entities

|Name|Description|
|-|-|
|ADDRESS| Simple entity matching addresses |
|KEYWORD| Simple entity matching point of interest keywords and categories |
|ROUTE_TYPE| Phrase list entity mapping route descriptors to `eco`,`fastest`,`shortest`,`thrilling`|
|number| Prebuilt entity|

## Event Responses

The Point of Interest Skill surfaces a users request to navigate to a new destination through an event returned to the client. The event is called `ActiveRoute.Directions" has contains a series of Points for the Route along with a summary of the route information. A simplified example is shown below

```json
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

## Configuration

### Supported Sources

> **Mandatory**: [Azure Maps](https://azure.microsoft.com/en-us/services/azure-maps/) is supported for finding Points of Interest and getting route directions to a selected location.
> As this is the only supported provider to get directions, this provider is required.

> [Foursquare](https://developer.foursquare.com/docs/api) is supported for finding Points of Interest and related details (rating, business hours, price level, etc.).

### Auth Connection Settings

> No Authentication is required for this skill

### Skill Parameters

The following Parameters are accepted by the Skill and enable additional personalization of responses to a given user:

- `IPA.Location` (*The skill will fail without this as it is missing a user's current coordinates*)
- To ease testing scenarios you can send the following message to pass a location enabling you to test the POI skill and adjust the location
  - `/event:{ "Name": "IPA.Location", "Value": "34.05222222222222,-118.24277777777778" }`

Read [Handling Events With Your Virtual Assistant]({{site.baseurl}}/reference/virtual-assistant/events) to learn how to manage events within a Skill.

### Configuration File Information

The following Configuration entries are required to be passed to the Skill and are provided through the Virtual Assistant appSettings.json file. These should be updated to reflect your LUIS deployment.

- `LuisAppId`
- `LuisSubscriptionKey`
- `LuisEndpoint`
- `AzureMapsKey`
- `FoursquareClientId` *(optional)*
- `FoursquareClientSecret` *(optional)*
- `ImageAssetLocation`
- `Radius` *(in meters)*
- `LimitSize`

### Example Skill Registration Entry

```json
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
    "AzureMapsKey": "YOUR_AZURE_MAPS_KEY",
    "FoursquareClientId": "YOUR_FOURSQUARE_CLIENT_ID",
    "FoursquareClientSecret": "YOUR_FOURSQUARE_CLIENT_SECRET",
    "Radius": "SEARCH_RADIUS_FROM_LOCATION",
    "ImageAssetLocation": "http://www.contoso.com/images/",
    "LimitSize": "POI_SEARCH_LIMIT"
    }
}
```

### Image Assets

In order for Adaptive Cards to render images associated with the Point of Interest skill you will need to take the image assets located in the wwwroot/images folder of the PointOfInterestSkill project and place in a HTTP location (potentially your Bot deployment) and place the base URI path in the skill configuration ImageAssetLocation property.
If you skip this step, Adaptive Cards will not render with images correctly.

### Deploying the Skill

1. Run **PowerShell Core** (pwsh.exe) and **change directory to the project directory** of your project.
2. Run the following command:

    ```shell
    ./Deployment/Scripts/deploy.ps1
    ```

    ### What do these parameters mean?

    Parameter | Description | Required
    --------- | ----------- | --------
    `name` | **Unique** name for your bot. By default this name will be used as the base name for all your Azure Resources and must be unique across Azure so ensure you prefix with something unique and **not** *MyAssistant* | **Yes**
    `location` | The region for your Azure Resources. By default, this will be the location for all your Azure Resources | **Yes**
    `appPassword` | The password for the [Azure Active Directory App](https://ms.portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/Overview) that will be used by your bot. It must be at least 16 characters long, contain at least 1 special character, and contain at least 1 numeric character. If using an existing app, this must be the existing password. | **Yes**
    `luisAuthoringKey` | The authoring key for your LUIS account. It can be found at https://www.luis.ai/user/settings or https://eu.luis.ai/user/settings | **Yes**

You can find more detailed deployment steps including customization in the [Virtual Assistant and Skill Template deployment]({{site.baseurl}}/reference/virtual-assistant/deploymentscripts) page.
