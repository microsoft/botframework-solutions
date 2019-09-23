---
category: Reference
subcategory: Skills
title: Experimental Skills
description: News, Search, Reservation, Weather, Music, Events, and Hospitality.
order: 12
---

# {{ page.title }}
{:.no_toc}

## In this reference
{:.no_toc}

* 
{:toc}

## Overview

These experimental Bot Framework Skills are early prototypes to help bring skill concepts to life for demonstrations and proof-of-concepts along with providing different examples to get you started.

These skills by their very nature are not complete, will likely have rudimentary language models, limited language support and limited testing hence are located in a experimental folder to ensure this is understood before you make use of them.

### Skill Deployment

The Experimental Skills require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

**To deploy the experimental skills using the default configuration, follow the steps in this common [deployment documentation page]({{site.baseurl}}/tutorials/csharp/create-assistant/4_provision_your_azure_resources) from the folder where your have cloned the GitHub repo.**

## Skills

### Bing Search Skill

The [Bing Search Skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/bingsearchskill) provides a simple Skill that integrates with the [Bing Search Cognitive Service](https://azure.microsoft.com/en-us/services/cognitive-services/bing-web-search-api/).

This skill has a very limited LUIS model (available in English, French, Italian, German, Spanish and Chinese) and demonstates three simple scenarios:

- Celebrity Information: *Who is Tom Cruise?*
- Q&A: *What is the gdp of switzerland*
- Movie Information: *Tell me about the jurassic park movie*

![Search Example]({{site.baseurl}}/assets/images/skills-experimental-bingsearch.png)

#### Configuration

1. Get your own [Bing Search Cognitive Services Key](https://azure.microsoft.com/en-us/services/cognitive-services/bing-web-search-api/).
1. Get your own [Project Answer Search Key](https://labs.cognitive.microsoft.com/en-us/project-answer-search).
1. Provide these values in your `appsettings.json` file.

```
"BingSearchKey": "{YOUR_BING_SEARCH_COGNITIVE_SERVICES_KEY}",
"BingAnswerSearchKey": "{YOUR_PROJECT_ANSWER_SEARCH_KEY}"
```

### Event Skill

The [Event Skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/eventskill) provides a simple skill that integrates with [Eventbrite](https://www.eventbrite.com/platform/) to show information about events happening in the specified area.

This skill currently supports one scenario to get local event information.

![Event Example]({{site.baseurl}}/assets/images/skills-event-transcript.png)

#### Configuration

1. Get your own [Eventbrite API Key](https://www.eventbrite.com/platform/api-keys).
1. Provide this value in your `appsettings.json` file.

```
"eventbriteKey":  "YOUR_EVENTBRITE_API_KEY"
```

### Hospitality Skill

The [Hospitality Skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/hospitalityskill) demonstrates experiences that would be useful in a hospitality specific scenario, such as being able to check out of a hotel, ordering room service, and requesting hotel amenities. This skill does not integrate a hotel service at this time, and is instead simulated with static data for testing purposes.

This skill demonstrates the following scenarios:
- Show reservation: *What is my current check out date?*
- Extend reservation: *Can I extend my stay?*
- Request late check-out: *I want a late check out time* 
- Request amenities: *Can you bring me a toothbrush and toothpaste?*
- Room service: *I want to see a room service menu*
- Check out: *Can I check out now?*

![Hospitality Example]({{site.baseurl}}/assets/images/skills-hospitality-transcript.png)

The [Hospitality Sample VA]({{site.baseurl}}/reference/samples/hospitalitysample) demonstrates this skill and a number of other skills to demonstrate a more in-depth hospitality experience.

### IT Service Management Skill

The [IT Service Management skill](https://github.com/microsoft/AI/tree/next/skills/src/csharp/experimental/itsmskill) provides a basic skill that provides ticket and knowledge base related capabilities and supports SerivceNow.

#### Configuration

To test this skill, one should setup the following:

1. Create a ServiceNow instance in the [ServiceNow Developer Site](https://developer.servicenow.com/app.do#!/instance).
1. Provide this value in your `appsettings.json` file.
`"serviceNowUrl": "{YOUR_SERVICENOW_INSTANCE_URL}`
1. Create a [scripted REST API](https://docs.servicenow.com/bundle/geneva-servicenow-platform/page/integrate/custom_web_services/task/t_CreateAScriptedRESTService.html) to get current user's sys_id and please raise an issue if simpler way is found
    - In System Web Services/Scripted REST APIs, click New to create an API
    - In API's Resources, click New to add a resource
    - In the resource, select GET for HTTP method and input `(function process(/*RESTAPIRequest*/ request, /*RESTAPIResponse*/ response) { return gs.getUserID(); })(request, response);` in Script
    - Update the serviceNowGetUserId of appsetting.json: `"serviceNowGetUserId": "YOUR_API_NAMESPACE/YOUR_API_ID"`
1. Set up endpoint by [this document](https://docs.servicenow.com/bundle/london-platform-administration/page/administer/security/task/t_CreateEndpointforExternalClients.html#t_CreateEndpointforExternalClients) for Client id and Client secret to be used in the following OAuth Connection
    - Redirect URL is https://token.botframework.com/.auth/web/redirect
1. Add an OAuth Connection in the Settings of Web App Bot named 'ServiceNow' with Service Provider 'Generic Oauth 2'
    - Authorization URL as https://instance.service-now.com/oauth_auth.do
    - Token URL, Refresh URL as https://instance.service-now.com/oauth_token.do
    - No Scopes are needed
    - Click Test Connection to verify

To test this skill in VA, one should setup the following:

1. Add https://botbuilder.myget.org/F/aitemplates/api/v3/index.json as NuGet package source
1. Update VA's Microsoft.Bot.Builder.Solutions and Microsoft.Bot.Builder.Skills to 4.6.0-daily27 as this skill
1. Add VA's appId to AppsWhitelist of SimpleWhitelistAuthenticationProvider under Utilities
1. Add OAuth Connection as skill
1. The remaining steps are same as normal skills

### Music Skill

The [Music skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/musicskill) integrates with [Spotify](https://developer.spotify.com/documentation/web-api/libraries/) to look up playlists and artists and open the Spotify app via URI.
This is dependent on the [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) wrapper for the Spotify Web API.

#### Configuration

1. Get your own client id and secret when you [create a Spotify client](https://developer.spotify.com/dashboard/).
1. Provide these values in your `appsettings.json` file.

```
  "spotifyClientId": "{YOUR_SPOTIFY_CLIENT_ID}",
  "spotifyClientSecret": "{YOUR_SPOTIFY_CLIENT_SECRET}"
```

#### Event Activity integration

This Skill supports an outgoing `OpenDefaultApp` Event Activity that provides a Spotify URI for chat clients to open on their own.

```
{ 
   "type":"event",
   "name":"OpenDefaultApp",
   "value":{ 
      "MusicUri":"{SPOTIFY_URI}"
   }
}
```


### News Skill

The [News skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/newsskill) provides a simple Skill that integrates with the Bing News Cognitive Service to demonstrate how a news experience can be integrated into a Virtual Assistant.

Once deployed create a [Bing News Cognitive Services Key](https://azure.microsoft.com/en-us/services/cognitive-services/bing-news-search-api/) and update the appropriate configuration within appSettings.config.

This skill supports the following scenarios:
- Find articles: *Find me news about sports*
- Trending articles: *What news is trending now?*
- Show favorite topic: *Find news for me*

![News Example]({{site.baseurl}}/assets/images/skills-news-transcript.png)

#### Configuration

1. Get your own [Bing News Cognitive Services Key](https://azure.microsoft.com/en-us/services/cognitive-services/bing-news-search-api/)
1. Provide this value in your `appsettings.json` file.

```
"BingNewsKey": "{YOUR_BING_NEWS_COGNITIVE_SERVICES_KEY}"
```

### Restaurant Booking Skill

The [Restaurant Booking skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/restaurantbooking) provides a simple restaurant booking experience guiding the user through booking a table and leverages Adaptive Cards throughout to demonstrate how Speech, Text and UX can be combined for a compelling user experience. No integration to restaurant booking services exists at this time so is simulated with static data for testing purposes.

![Restaurant Example]({{site.baseurl}}/assets/images/skills-restaurant-transcript.png)

### Weather Skill

The [Weather skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/weatherskill) provides a basic Skill that integrates with [AccuWeather](https://developer.accuweather.com) to demonstrate how a weather experience can be integrated into a Virtual Assistant.

#### Configuration

1. Get your own API Key when by following the instructions on [AccuWeather Getting Started](https://developer.accuweather.com/getting-started).
1. Provide this value in your `appsettings.json` file.

```
"WeatherApiKey": "{YOUR_ACCUWEATHER_API_KEY}"
```
