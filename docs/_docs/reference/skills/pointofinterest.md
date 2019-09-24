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

## Supported scenarios
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

## Language Understanding (LUIS)
LUIS models are provided in `.lu` file format to support the scenarios used in this Skill.

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
|GetDirections| Matches queries navigating to a point of interest |
|FindPointOfInterest| Matches queries searching for a point of interest |
|FindParking| Matches queries searching for a parking space |

### Entities

|Name|Description|
|-|-|
|Address| Simple entity matching addresses |
|Keyword| Simple entity matching point of interest keywords and categories |
|RouteDescription| Phrase list entity mapping route descriptors to "eco","fastest","shortest","thrilling"|
|PoiDescription| Phrase list entity mapping descriptors like "nearest" or "closest"|
|Keyword_brand| Common point of interest brands |
|Keyword_cuisine| Common point of interest cuisines|
|Keyword_category| Common point of interest categories|
|number| Prebuilt entity|

## Configuration
### Deployment
Learn how to [provision your Azure resources]({{site.baseurl}}/tutorials/csharp/create-skill/4_provision_your_azure_resources/) in the Create a Skill tutorial.

### Supported content providers
> **Mandatory**: [Azure Maps](https://azure.microsoft.com/en-us/services/azure-maps/) is supported for finding Points of Interest and getting route directions to a selected location.
> As this is the only supported provider to get directions, this provider is required.

> [Foursquare](https://developer.foursquare.com/docs/api) is supported for finding Points of Interest and related details (rating, business hours, price level, etc.).

### Authentication connection settings
> No authentication is required for this skill

## Events
Learn how to use [events]({{site.baseurl}}/reference/virtual-assistant/events) to send backend data to a Skill, like a user's location.

### From assistant to user
This Skill supports an outgoing `OpenDefaultApp` Event Activity that provides a Geo URI for chat clients to determine how to handle navigation to a user's selected point of interest.
The [Virtual Assistant Client (Android) sample]({{ site.baseurl }}/howto/samples/vaclient_android/) demonstrates how a client may navigate to a destination using a user's preferred map application.

```json
{ 
   "type":"event",
   "name":"OpenDefaultApp",
   "value":{ 
      "GeoUri":"geo:{LONGITUDE},{LATITUDE}"
   }
}
```