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

[Experimental Skills](https://aka.ms/bfexperimentalskills) are early prototypes of Skills to help bring skill concepts to life for demonstrations and proof-of-concepts along with providing different examples to get you started.

These skills by their very nature are not complete, will likely have rudimentary language models, limited language support and limited testing hence are located in a experimental folder to ensure this is understood before you make use of them.

## Restaurant Booking Skill

The [Restaurant Booking skill](https://github.com/microsoft/AI/tree/master/skills/src/csharp/experimental/restaurantbooking) provides a simple restaurant booking experience guiding the user through booking a table and leverages Adaptive Cards throughout to demonstrate how Speech, Text and UX can be combined for a compelling user experience. No integration to restaurant booking services exists at this time so is simulated with static data for testing purposes.

![Restaurant Example]({{site.baseurl}}/assets/images/skills-restaurant-transcript.png)

## News Skill

The [News skill](https://github.com/microsoft/AI/tree/master/skills/src/csharp/experimental/newsskill) provides a simple Skill that integrates with the Bing News Cognitive Service to demonstrate how a news experience can be integrated into a Virtual Assistant.

Once deployed create a [Bing News Cognitive Services Key](https://azure.microsoft.com/en-us/services/cognitive-services/bing-news-search-api/) and update the appropriate configuration within appSettings.config.

This skill supports the following scenarios:
- Find articles: *Find me news about sports*
- Trending articles: *What news is trending now?*
- Show favorite topic: *Find news for me*

![News Example]({{site.baseurl}}/assets/images/skills-news-transcript.png)

## Weather Skill

The [Weather skill](https://github.com/microsoft/AI/tree/master/skills/src/csharp/experimental/weatherskill) provides a basic Skill that integrates with [AccuWeather](https://developer.accuweather.com) to demonstrate how a weather experience can be integrated into a Virtual Assistant. Provide an API key from [AccuWeather Getting Started](https://developer.accuweather.com/getting-started) in the appsettings to configure the skill.

## Music Skill

The [Music skill](https://github.com/microsoft/AI/tree/master/skills/src/csharp/experimental/musicskill) integrates with [Spotify](https://developer.spotify.com/documentation/web-api/libraries/) to look up playlists and artists and open via the Spotify app. Provide credentials after you [create a Spotify client](https://developer.spotify.com/dashboard/) in the appsettings to configure the skill.

## Bing Search Skill

The [Bing Search Skill](https://github.com/microsoft/AI/tree/master/skills/src/csharp/experimental/bingsearchskill) provides a simple Skill that integrates with the [Bing Search cognitive service](https://azure.microsoft.com/en-us/services/cognitive-services/bing-web-search-api/).

This skill has a very limited LUIS model (available in English, French, Italian, German, Spanish and Chinese) and demonstates three simple scenarios:

- Celebrity Information: *Who is Tom Cruise?*
- Q&A: *What is the gdp of switzerland*
- Movie Information: *Tell me about the jurassic park movie*

![Search Example]({{site.baseurl}}/assets/images/skills-experimental-bingsearch.png)

Once deployed create a [Bing Search Cognitive Services Key](https://azure.microsoft.com/en-us/services/cognitive-services/bing-web-search-api/) for BingSearchKey and a [Project Answer Search Key](https://labs.cognitive.microsoft.com/en-us/project-answer-search) for BingAnswerSearchKey, then update the appropriate configuration within appSettings.config.

## Event Skill

The [Event Skill](https://github.com/microsoft/AI/tree/master/skills/src/csharp/experimental/eventskill) provides a simple skill that integrates with [Eventbrite](https://www.eventbrite.com/platform/) to show information about events happening in the specified area. Provide an API key from the [Eventbrite API Keys](https://www.eventbrite.com/platform/api-keys) page in the appsettings to configure the skill.

This skill currently supports one scenario to get local event information.

![Event Example]({{site.baseurl}}/assets/images/skills-event-transcript.png)

## Hospitality Skill

The [Hospitality Skill](https://github.com/microsoft/AI/tree/master/skills/src/csharp/experimental/hospitalityskill) demonstrates experiences that would be useful in a hospitality specific scenario, such as being able to check out of a hotel, ordering room service, and requesting hotel amenities. This skill does not integrate a hotel service at this time, and is instead simulated with static data for testing purposes.

This skill demonstrates the following scenarios:
- Show reservation: *What is my current check out date?*
- Extend reservation: *Can I extend my stay?*
- Request late check-out: *I want a late check out time* 
- Request amenities: *Can you bring me a toothbrush and toothpaste?*
- Room service: *I want to see a room service menu*
- Check out: *Can I check out now?*

The [Hospitality Sample VA](https://github.com/microsoft/AI/tree/master/solutions/HospitalitySample) leverages this skill and a number of other skills to demonstrate a more in-depth hospitality experience.

![Hospitality Example]({{site.baseurl}}/assets/images/skills-hospitality-transcript.png)

## Experimental Skill Deployment

The Experimental Skills require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

**To deploy the experimental skills using the default configuration, follow the steps in this common [deployment documentation page]({{site.baseurl}}/tutorials/csharp/create-assistant/4_provision_your_azure_resources) from the folder where your have cloned the GitHub repo.**
