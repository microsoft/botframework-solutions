---
category: Skills
subcategory: Samples
title: Point Of Interest Skill
description: Find points of interest and directions. Powered by Azure Maps and FourSquare.
order: 3
toc: true
---

# {{ page.title }}
{:.no_toc}

{{ page.description }}

## Supported scenarios
{:.toc}

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
  - _Can you recommend a restaurant in Seattle?_

## Language Understanding
{:.toc}

LUIS models are provided in **.lu** file format to support the scenarios used in this Skill.

|Supported Languages|
|-|
|English|
|French|
|Italian|
|German|
|Spanish|
|Chinese (simplified)|

### Intents
{:.no_toc}

|Name|Description|
|-|-|
|GetDirections| Matches queries navigating to a point of interest |
|FindPointOfInterest| Matches queries searching for a point of interest |
|FindParking| Matches queries searching for a parking space |

### Entities
{:.no_toc}

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
{:.toc}

### Deployment
{:.no_toc}

Learn how to [provision your Azure resources]({{site.baseurl}}/skills/tutorials/create-skill/csharp/4-provision-your-azure-resources) in the Create a Skill tutorial.

### Supported content providers
{:.no_toc}

> **Mandatory**: [Azure Maps](https://azure.microsoft.com/en-us/services/azure-maps/) is supported for finding Points of Interest and getting route directions to a selected location.
> As this is the only supported provider to get directions, this provider is required.

> [Foursquare](https://developer.foursquare.com/docs/api) is supported for finding Points of Interest and related details (rating, business hours, price level, etc.).

### Authentication connection settings
{:.no_toc}

> No authentication is required for this skill

## Events
{:.toc}

Learn how to use [events]({{site.baseurl}}/virtual-assistant/handbook/events) to send backend data to a Skill, like a user's location.

### From assistant to user
{:.no_toc}

This Skill supports an outgoing **OpenDefaultApp** Event Activity that provides a [Geo URI](https://en.wikipedia.org/wiki/Geo_URI_scheme) for chat clients to determine how to handle navigation to a user's selected point of interest.
The [Virtual Assistant Client (Android) sample]({{site.baseurl}}/clients-and-channels/clients/virtual-assistant-client) demonstrates how a client may navigate to a destination using a user's preferred map application.

```json
{ 
   "type":"event",
   "name":"OpenDefaultApp",
   "value":{ 
      "GeoUri":"geo:{LATITUDE},{LONGITUDE}"
   }
}
```

## Download a transcript

<a class="btn btn-primary" href="{{site.baseurl}}/assets/transcripts/skills-pointofinterest.transcript">Download</a>