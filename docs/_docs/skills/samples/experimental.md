---
category: Skills
subcategory: Samples
title: Experimental Skills
description: These experimental Bot Framework Skills are early prototypes to help bring skill concepts to life for demonstrations and proof-of-concepts along with providing different examples to get you started. These skills by their very nature are not complete, will likely have rudimentary language models, limited language support and limited testing hence are located in a experimental folder to ensure this is understood before you make use of them.
order: 5
toc: true
---

# {{ page.title }}
{:.no_toc}

### Skill Deployment
{:.toc}

The Experimental Skills require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding

> Review the pricing and terms for the services and adjust to suit your scenario.

**To deploy the experimental skills using the default configuration, follow the steps in this common [deployment documentation page]({{site.baseurl}}/tutorials/csharp/create-assistant/4_provision_your_azure_resources) from the folder where your have cloned the GitHub repo.**

## Skills
{:.no_toc}

### Automotive Skill
{:.toc}

The Automotive Skill is in preview and demonstrates the first capabilities to help enable Automotive scenarios. The skill focuses on Vehicle Settings, specifically Climate Control, Safety and Basic audio controls. Media, Tuner and Phone capabilities are expected in a future release.

Vehicle Control is a complicated domain, whilst there are only a limited set of car controls for climate control there are a myriad of ways that a human can describe a given setting. For example, *I'm feeling chilly* , *My feet are cold* and *It's cold here in the back* all relate to a decrease in temperature but to different parts of the car and perhaps even different fan settings.

The Skill leverages a set of LUIS models to help understand the intent and entities but then leverages capabilities from our Maluuba team to match potential settings and actions to the available settings to then suggest a course of action.

Unlike the Productivity and PoI skills that are integrated into existing services, the automotive skill will require integration with the telematics solution in use by a given OEM so will require customization to reflect actual car features for a given OEM along with integration.

To enable testing and simulation any action identified is surfaced to the calling application as an event, this can easily be seen within the Bot Framework Emulator and will be wired up into the Web Test harness available as part of the Virtual Assistant solution.

#### Supported scenarios
{:.no_toc}

At this time, changes to vehicle settings are supported through the `VEHICLE_SETTINGS_CHANGE` and `VEHICLE_SETTINGS_DECLARATIVE` intents. The former enables questions such as "change the temperature to 21 degrees" whereas the latter intent enables scenarios such as "I'm feeling cold" which require additional processing steps.

The following vehicle setting areas are supported at this time, example utterances are provided for guidance. In cases where the utterance results in multiple potential settings or a value isn't provided then the skill will prompt for disambiguation. Confirmation will be sought from the user if a setting is configured to require confirmation, important for sensitive settings such as safety.

##### Climate Control
{:.no_toc}

- *Set temperature to 21 degrees*
- *Defog my windshield*
- *Put the air on my feet*
- *Turn off the ac*
- *I'm feeling cold*
- *It's feeling cold in the back*
- *The passenger is freezing*
- *Change climate control*

##### Safety
{:.no_toc}

- *Turn lane assist off*
- *Enable lane change alert*
- *Set park assist to alert*

##### Audio
{:.no_toc}

- *Adjust the equalizer*
- *Increase the bass*
- *Increase the volume*

Vehicle settings can be selected through explicit entry of the vehicle setting name, numeric or ordinal (first one, last one).

An example transcript file demonstrating the Skill in action can be found [here]({{site.baseurl}}/assets/transcripts/skills-automotive.transcript), you can use the Bot Framework Emulator to open transcripts.

![ Automotive Skill Transcript Example]({{site.baseurl}}/assets/images/skills-auto-transcript.png)

#### Language Understanding
{:.no_toc}

LUIS models for the Skill are provided in `.lu` file format as part of the Skill. These are currently available in English with other languages to follow.

The following Top Level intents are available with the main `settings` LUIS model

- VEHICLE_SETTINGS_CHANGE
- VEHICLE_SETTINGS_DECLARATIVE

In addition there are two supporting LUIS models `settings_name` and `settings_value`, these are used for disambiguation scenarios to clarify setting names and values where the initial utterance doesn't provide clear information.

#### Configuration
{:.no_toc}

##### Customizing vehicle settings
{:.no_toc}

Available vehicle settings are defined in a supporting metadata file which you can find in this location:  `automotiveskill/Dialogs/VehicleSettings/Resources/available_settings.yaml`.

To add an new setting along with appropriate setting values it's easily expressed in YAML. The example below shows a new Volume control setting with the ability to Set, Increase, Decrease and Mute the volume.

```
canonicalName: Volume
values:
  - canonicalName: Set
    requiresAmount: true
  - canonicalName: Decrease
    changesSignOfAmount: true
  - canonicalName: Increase
    antonym: Decrease
  - canonicalName: Mute
allowsAmount: true
amounts:
  - unit: ''
```

 For key settings you may wish to prompt for confirmation, safety settings for example. This can be specified through a `requiresConfirmation` property as shown below.

```
canonicalName: Lane Change Alert
values:
  - canonicalName: Off
    requiresConfirmation: true
  - canonicalName: On
```

##### Deploying the Skill in local-mode
{:.no_toc}

The Automotive skill is not added by default when deploying the Virtual Assistant as this is a domain specific skill.

Run this PowerShell script to deploy your shared resources and LUIS models.

```
  pwsh.exe -ExecutionPolicy Bypass -File DeploymentScripts/deploy_bot.ps1
```

You will be prompted to provide the following parameters:

- Name - A name for your bot and resource group. This must be **unique**.
- Location - The Azure region for your services (e.g. westus)
- LUIS Authoring Key - Refer to [this documentation page]({{site.baseurl}}/tutorials/csharp/create-assistant/1_intro) for retrieving this key.

The MSBot tool will outline the deployment plan including location and SKU. Ensure you review before proceeding.

> After deployment is complete, it's **imperative** that you make a note of the .bot file secret provided as this will be required for later steps. The secret can be found near the top of the execution output and will be in purple text.

- Update your `appsettings.json` file with the newly created `.bot` file name and `.bot` file secret.
- Run the following command and retrieve the InstrumentationKey for your Application Insights instance and update `InstrumentationKey` in your `appsettings.json` file.

```
msbot list --bot YOURBOTFILE.bot --secret YOUR_BOT_SECRET
```

```json
{
  "botFilePath": ".//YOURBOTFILE.bot",
  "botFileSecret": "YOUR_BOT_SECRET",
  "ApplicationInsights": {
    "InstrumentationKey": "YOUR_INSTRUMENTATION_KEY"
  }
}
```

- Finally, add the `.bot` file paths for each of your language configurations (English only at this time).

```json
"defaultLocale": "en-us",
"languageModels": {
  "en": {
    "botFilePath": ".//LocaleConfigurations//YOUR_EN_BOT_PATH.bot",
    "botFileSecret": ""
  }
}
```

Once you have followed the deployment instructions above, open the provided `.bot` file with the Bot Framework Emulator.

##### Adding the Skill to an existing Virtual Assistant deployment
{:.no_toc}

Follow the instructions below to add the Automotive Skill to an existing Virtual Assistant deployment that you have.

1. Update the Virtual Assistant deployment scripts.
    - Add the additional automotive skill LUIS models to the bot.recipe file located within your assistant project: `assistant/DeploymentScripts/en/bot.recipe`

		```json
		{
			"type": "luis",
			"id": "settings",
			"name": "settings",
			"luPath": "../skills/automotiveskill/automotiveskill/CognitiveModels/LUIS/en/settings.lu"
		},
		{
			"type": "luis",
			"id": "settings_name",
			"name": "settings_name",
			"luPath": "../skills/automotiveskill/automotiveskill/CognitiveModels/LUIS/en/settings_name.lu"
		},
		{
			"type": "luis",
			"id": "settings_value",
			"name": "settings_value",
			"luPath": "../skills/automotiveskill/automotiveskill/CognitiveModels/LUIS/en/settings_value.lu"
		},
		```

	- Add dispatch references to the core LUIS intents for the skill within the **assistant/CognitiveModels/en/dispatch.lu** file as shown below. Only the vehicle settings model is required for dispatch. This enables the Dispatcher to understand your new capabilities and route utterances to your skill
    
		```
		# l_Automotive
		- [VEHICLE_SETTINGS_CHANGE](../../../../skills/automotiveskill/automotiveskill/CognitiveModels/LUIS/en/settings_dispatch.lu#VEHICLE_SETTINGS_CHANGE)
		```

2. Run the following script to deploy the new Automotive Skill LUIS models and to update the dispatcher.

    ```
    pwsh.exe -ExecutionPolicy Bypass -File DeploymentScripts/update_published_models.ps1 -locales "en-us"
    ```

3. In Virtual Assistant, add the skill configuration entry (in an earlier section) to **appsettings.json**. This tells the Virtual Assistant that there is a new skill available for use.

4. Run the LuisGen tool to update the strongly-typed Dispatch class (Dispatch.cs) to reflect the additional dispatch target.

    ```
    LUISGen DeploymentScripts/en/dispatch.luis -cs Dispatch -o Dialogs/Shared/Resources
    ```

5. Update **MainDialog.cs** within your Assistant project with the dispatch intent for your skill (l_automotive). This can be found in the assistant/dialogs/main folder of your project.
    ![Add My Skill Image]({{site.baseurl}}/assets/images/skills_maindialogupdate.jpg)

6. Add a project reference from your Virtual Assistant project to the Automotive Skill, this will ensure the DLL housing the skill can be found at runtime for skill activation.

7. In order for Adaptive Cards to render images associated with the Automotive skill you will need to take the Image assets located in the `wwwroot/images` folder of the Automotive skill and place in a HTTP location (potentially your Bot deployment) and place the base URI path in the skill configuration `ImageAssetLocation` property. If you skip this step, Adaptive Cards will not render with images correctly.

#### Events
{:.no_toc}

The Automotive Skill surfaces setting changes for testing purposes through an event returned to the client. This enables easy testing and simulation, all events are prefixed with `AutomotiveSkill.`. The below event is generated as a response to `I'm feeling cold`

```json
{
  "name": "AutomotiveSkill.Temperature",
  "type": "event",
  "value": [
    {
      "Key": "valueingform",
      "Value": "Increasing"
    },
    {
      "Key": "settingname",
      "Value": "Temperature"
    }
  ]
}
```

### Bing Search Skill

The [Bing Search Skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/bingsearchskill) provides a simple Skill that integrates with the [Bing Search Cognitive Service](https://azure.microsoft.com/en-us/services/cognitive-services/bing-web-search-api/).

This skill has a very limited LUIS model (available in English, French, Italian, German, Spanish and Chinese) and demonstates three simple scenarios:

- Celebrity Information: *Who is Tom Cruise?*
- Q&A: *What is the gdp of switzerland*
- Movie Information: *Tell me about the jurassic park movie*

![Search Example]({{site.baseurl}}/assets/images/skills-experimental-bingsearch.png)

#### Configuration
{:.no_toc}

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
{:.no_toc}

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
{:.no_toc}

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
{:.no_toc}

1. Get your own client id and secret when you [create a Spotify client](https://developer.spotify.com/dashboard/).
1. Provide these values in your `appsettings.json` file.

```
  "spotifyClientId": "{YOUR_SPOTIFY_CLIENT_ID}",
  "spotifyClientSecret": "{YOUR_SPOTIFY_CLIENT_SECRET}"
```

#### Events
{:.no_toc}

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
{:.no_toc}

1. Get your own [Bing News Cognitive Services Key](https://azure.microsoft.com/en-us/services/cognitive-services/bing-news-search-api/)
1. Provide this value in your `appsettings.json` file.

```
"BingNewsKey": "{YOUR_BING_NEWS_COGNITIVE_SERVICES_KEY}"
```

### Phone Skill

The Phone Skill provides the capability to start phone calls to a Virtual Assistant.

#### Supported scenarios
{:.no_toc}

The following scenarios are currently supported by the Skill:

- Outgoing Call
  - *Call Sanjay Narthwani*
  - *Call 555 5555*
  - *Make a call*

The skill will automatically prompt the user for any missing information and/or to clarify ambiguous information.

##### Example dialog
{:.no_toc}

Here is an example of a dialog with the Phone skill that showcases all possible prompts.
Note that the skill may skip prompts if the corresponding information is already given.
This example assumes that the user's contact list contains multiple contacts named "Sanjay", one of which is named "Sanjay Narthwani" and has multiple phone numbers, one of which is labelled "Mobile".

|Turn| Utterance/ Prompt |
|-|-|
|User| Make a call |
|Skill| Who would you like to call? |
|User| Sanjay |
|Skill| Which Sanjay? |
|User| Narthwani |
|Skill| Sanjay Narthwani has multiple phone numbers. Which one? |
|User| Mobile |
|Skill| Calling Sanjay Narthwani on mobile. |

Refer to the unit tests for further example dialogs.

#### Language Understanding
{:.no_toc}

LUIS models for the Skill are provided in `.lu` file format as part of the Skill. Further languages are being prioritized.

|Supported Languages|
|-|
|English|

The LUIS model `phone` is used to understand the user's initial query as well as responses to the prompt "Who would you like to call?"
The other LUIS models (`contactSelection` and `phoneNumberSelection`) are used to understand the user's responses to later prompts in the dialog.

##### Intents
{:.no_toc}

|Name|Description|
|-|-|
|OutgoingCall| Matches queries to make a phone call |

##### Entities
{:.no_toc}

|Name|Description|
|-|-|
|contactName| The name of the contact to call |
|phoneNumber| A literal phone number specified by the user in the query, in digits |
|phoneNumberSpelledOut| A literal phone number specified by the user in the query, in words |
|phoneNumberType| Identifies a certain phone number of the contact by its type (for example, "home", "business", "mobile") |

#### Configuration 
{:.no_toc}

##### Supported content providers
{:.no_toc}

> Office 365 and Outlook.com through the Microsoft Graph is supported along with support for Google accounts.

To use Google account in skill you need to follow these steps:
1. Create your Gmail API credential in [Google developers console](https://console.developers.google.com).
2. Create an OAuth connection setting in your Web App Bot.
    - Connection name: `googleapi`
    - Service Provider: `Google`
    - Client id and secret are generated in step 1
    - Scopes: `"https://www.googleapis.com/auth/contacts"`.
3. Add the connection name, client id, secret and scopes in appsetting.json file.

##### Authentication connection settings
{:.no_toc}

If you plan to use the skill as part of a Virtual Assistant, the process of registering a skill with your Virtual Assistant will create the supporting authentication connection information automatically for your Virtual Assistant. This skill uses the following authentication scopes, which are registered automatically:
- `User.ReadBasic.All`
- `User.Read`
- `People.Read`
- `Contacts.Read`

**However**, if you wish to use the Skill directly without using a Virtual Assistant, please use the following steps to manually configure Authentication for the Phone Skill. This is **not** required when using the Skill with a Virtual Assistant.

Follow the general instructions [here]({{site.baseurl}}/howto/skills/manualauthsteps.md) to configure this using the scopes shown above.

#### Events
{:.no_toc}

Note that the Phone skill only handles the dialog with the user about the phone call to be made, but does not place the actual phone call.
The phone call would typically be placed by the client application communicating with the bot or skill.
For example, if the client application is an Android app, it would communicate with the bot to allow the user to go through the dialog and at the end, it would place the call using an Android mechanism for placing calls.

The information that is required to place the call is returned from the Phone skill in the form of an event at the end of the dialog.
This event has the name `PhoneSkill.OutgoingCall`.
Its value is a JSON object representing an object of type `PhoneSkill.Models.OutgoingCall`.

The value of the event has the following properties:
- The property `Number` holds the phone number to be dialed as a string.
  (Please note that this string is in the same format as it appears in the user's contact list or in the user's query.
  If you require an RFC 3966 compliant `tel:` URI or a particular other format, we recommend using a phone number formatting library to format this string accordingly, taking into account the user's default country code and any other relevant external information.)
- The property `Contact` is optional and holds the contact list entry that the user selected.
  This is an object of type `PhoneSkill.Models.ContactCandidate`.
  This information may be useful, for example, to allow the client application to show information about the contact on the screen while the phone number is being dialed.

Here is an example of an event returned by the Phone skill:

```
{
    [...]
    "type": "event",
    "name": "PhoneSkill.OutgoingCall",
    "value": {
    "Number": "555 111 1111",
    "Contact": {
        "CorrespondingId": "[...]",
        "Name": "Andrew Smith",
        "PhoneNumbers": [
        {
            "Number": "555 111 1111",
            "Type": {
            "FreeForm": "",
            "Standardized": 1
            }
        }
        ]
    }
    }
}
```

### Restaurant Booking Skill

The [Restaurant Booking skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/restaurantbooking) provides a simple restaurant booking experience guiding the user through booking a table and leverages Adaptive Cards throughout to demonstrate how Speech, Text and UX can be combined for a compelling user experience. No integration to restaurant booking services exists at this time so is simulated with static data for testing purposes.

![Restaurant Example]({{site.baseurl}}/assets/images/skills-restaurant-transcript.png)

### Weather Skill

The [Weather skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/weatherskill) provides a basic Skill that integrates with [AccuWeather](https://developer.accuweather.com) to demonstrate how a weather experience can be integrated into a Virtual Assistant.

#### Configuration
{:.no_toc}

1. Get your own API Key when by following the instructions on [AccuWeather Getting Started](https://developer.accuweather.com/getting-started).
1. Provide this value in your `appsettings.json` file.

```
"WeatherApiKey": "{YOUR_ACCUWEATHER_API_KEY}"
```
