---
category: Reference
subcategory: Skills
title: Experimental Skills
description: News, Search, Reservation, and Weather.
order: 12
---

# {{ page.title }}
{:.no_toc}

## In this reference
{:.no_toc}

* 
{:toc}

## Overview

Experimental Skills are early prototypes of Skills to help bring skill concepts to life for demonstrations and proof-of-concepts along with providing different examples to get you started.

These skills by their very nature are not complete, will likely have rudimentary language models, limited language support and limited testing hence are located in a experimental folder to ensure this is understood before you make use of them.

## Restaurant Booking Skill

The [Restaurant Booking skill](https://github.com/microsoft/AI/tree/master/skills/src/csharp/experimental/restaurantbooking) provides a simple restaurant booking experience guiding the user through booking a table and leverages Adaptive Cards throughout to demonstrate how Speech, Text and UX can be combined for a compelling user experience. No integration to restaurant booking services exists at this time so is simulated with static data for testing purposes.

## News Skill

The [News skill](https://github.com/microsoft/AI/tree/master/skills/src/csharp/experimental/newsskill) provides a simple Skill that integrates with the Bing News Cognitive Service to demonstrate how a news experience can be integrated into a Virtual Assistant.

## Weather Skill

The Weather skill provides a basic Skill that integrates with [AccuWeather](https://developer.accuweather.com) to demonstrate how a weather experience can be integrated into a Virtual Assistant. Provide an API key from [AccuWeather Getting Started](https://developer.accuweather.com/getting-started) in the appsettings to configure the skill.

## Music Skill

The Music skill integrates with [Spotify](https://developer.spotify.com/documentation/web-api/libraries/) to look up playlists and artists and open via the Spotify app. Provide credentials after you [create a Spotify client](https://developer.spotify.com/dashboard/) in the appsettings to configure the skill.

## Experimental Skill Deployment

The Experimental Skills require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

**To deploy the experimental skills using the default configuration, follow the steps in this common [deployment documentation page]({{site.baseurl}}/tutorials/create-assistant/4_provision_your_azure_resources) from the folder where your have cloned the GitHub repo.**
