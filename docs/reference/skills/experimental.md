# Experimental Skills

## Overview

Experimental Skills are early prototypes of Skills to help bring skill concepts to life for demonstrations and proof-of-concepts along with providing different examples to get you started.

These skills by their very nature are not complete, will likely have rudimentary language models, limited language support and limited testing hence are located in a experimental folder to ensure this is understood before you make use of them.

## Experimental Skill Deployment

The Experimental Skills require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

**To deploy the experimental skills using the default configuration, follow the steps in this common [deployment documentation page](/docs/tutorials/assistantandskilldeploymentsteps.md) from the folder where your have cloned the GitHub repo.**

## Restaurant Booking Skill

The [Restaurant Booking skill](https://github.com/microsoft/AI/tree/master/skills/src/csharp/experimental/restaurantbooking) provides a simple restaurant booking experience guiding the user through booking a table and leverages Adaptive Cards throughout to demonstrate how Speech, Text and UX can be combined for a compelling user experience. No integration to restaurant booking services exists at this time so is simulated with static data for testing purposes.

![Restaurant Example](/docs/media/skills-restaurant-transcript.png)


## Bing Search Skill

The [Bing Search Skill](https://github.com/microsoft/AI/tree/master/skills/src/csharp/experimental/bingsearchskill) provides a simple Skill that integrates with the [Bing Search cognitive service](https://azure.microsoft.com/en-us/services/cognitive-services/bing-web-search-api/).

This skill has a very limited LUIS model (available in English, French, Italian, German, Spanish and Chinese) and demonstates three simple scenarios:

- Celebrity Information: *Who is Tom Cruise?*
- Q&A: *What is the gdp of switzerland*
- Movie Information: *Tell me about the jurassic park movie*

![Search Example](/docs/media/skills-experimental-bingsearch.png)

Once deployed create a [Bing Search Cognitive Services Key](https://azure.microsoft.com/en-us/services/cognitive-services/bing-web-search-api/) and update the appropriate configuration within appSettings.config.

## News Skill

The [News skill](https://github.com/microsoft/AI/tree/master/skills/src/csharp/experimental/newsskill) provides a simple Skill that integrates with the Bing News Cognitive Service to demonstrate how a news experience can be integrated into a Virtual Assistant.

Once deployed create a [Bing News Cognitive Services Key](https://azure.microsoft.com/en-us/services/cognitive-services/bing-news-search-api/) and update the appropriate configuration within appSettings.config.

![News Example](/docs/media/skills-news-transcript.png)

## Weather Skill

The Weather skill provides a basic Skill that integrates with [AccuWeather](https://developer.accuweather.com) to demonstrate how a weather experience can be integrated into a Virtual Assistant. Provide an API key from [AccuWeather Getting Started](https://developer.accuweather.com/getting-started) in the appsettings.config to configure the Skill.