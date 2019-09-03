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

## IT Service Management Skill

The [IT Service Management skill](https://github.com/microsoft/AI/tree/next/skills/src/csharp/experimental/itsmskill) provides a basic skill that provides ticket and knowledge base related capabilities and supports SerivceNow.

To test this skill, one should setup the following:

* Create a ServiceNow instance in [Developers](https://developer.servicenow.com/app.do#!/instance) and update the serviceNowUrl of appsettings.json: `"serviceNowUrl": "https://YOUR_INSTANCE_NAME.service-now.com"`
* Create a [scripted REST API](https://docs.servicenow.com/bundle/geneva-servicenow-platform/page/integrate/custom_web_services/task/t_CreateAScriptedRESTService.html) to get current user's sys_id and please raise an issue if simpler way is found
    - In System Web Services/Scripted REST APIs, click New to create an API
    - In API's Resources, click New to add a resource
    - In the resource, select GET for HTTP method and input `(function process(/*RESTAPIRequest*/ request, /*RESTAPIResponse*/ response) { return gs.getUserID(); })(request, response);` in Script
    - Update the serviceNowGetUserId of appsetting.json: `"serviceNowGetUserId": "YOUR_API_NAMESPACE/YOUR_API_ID"`
* Set up endpoint by [this document](https://docs.servicenow.com/bundle/london-platform-administration/page/administer/security/task/t_CreateEndpointforExternalClients.html#t_CreateEndpointforExternalClients) for Client id and Client secret to be used in the following OAuth Connection
    - Redirect URL is https://token.botframework.com/.auth/web/redirect
* Add an OAuth Connection in the Settings of Web App Bot named 'ServiceNow' with Service Provider 'Generic Oauth 2'
    - Authorization URL as https://instance.service-now.com/oauth_auth.do
    - Token URL, Refresh URL as https://instance.service-now.com/oauth_token.do
    - No Scopes are needed
    - Click Test Connection to verify

To test this skill in VA, one should setup the following:

* Add https://botbuilder.myget.org/F/aitemplates/api/v3/index.json as NuGet package source
* Update VA's Microsoft.Bot.Builder.Solutions and Microsoft.Bot.Builder.Skills to 4.6.0-daily27 as this skill
* Add VA's appId to AppsWhitelist of SimpleWhitelistAuthenticationProvider under Utilities
* Add OAuth Connection as skill
* The remaining steps are same as normal skills

## Experimental Skill Deployment

The Experimental Skills require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

**To deploy the experimental skills using the default configuration, follow the steps in this common [deployment documentation page]({{site.baseurl}}/tutorials/csharp/create-assistant/4_provision_your_azure_resources) from the folder where your have cloned the GitHub repo.**
