---
category: Virtual Assistant
subcategory: Handbook
title: Localization
description: Manage localization across a Virtual Assistant solution
order: 5
toc: true
---

# {{ page.title }} : {{ page.description }}
{:.no_toc}
{{ page.description }}

## Getting the locale
To capture the user's locale, the Virtual Assistant uses the SetLocaleMiddleware. For each message that comes in from the user, the CurrentUICulture is set equal to the Activity's locale property. If Activity.Locale is not available on the activity, the DefaultLocale from cognitivemodel.json is used instead.

## Cognitive Models
Each cognitive model used by the assistant (i.e. LUIS, QnA Maker, Dispatch) should be deployed in each language you want to support. The configuration for these models should be included in the cognitiveModels collection in cognitivemodels.json.

**Example cognitivemodels.json**
```
{
    "defaultLocale": "en-us"
    "cognitiveModels": {
        "en": {
            "dispatchModel": {
                "authoringkey": "",
                "appid": "",
                "name": "",
                "subscriptionkey": "",
                "region": "",
                "authoringRegion": ""
            },
            "languageModels": [
                {
                    "subscriptionkey": "",
                    "appid": "",
                    "id": "",
                    "version": "",
                    "region": "",
                    "name": "",
                    "authoringkey": "",
                    "authoringRegion": ""
                }
            ],
            "knowledgebases": [
                {
                    "endpointKey": "",
                    "kbId": "",
                    "hostname": "",
                    "subscriptionKey": "",
                    "name": "",
                    "id": ""
                }
            ]
        },
        "es": {
            "dispatchModel": {
                "authoringkey": "",
                "appid": "",
                "name": "",
                "subscriptionkey": "",
                "region": "",
                "authoringRegion": ""
            },
            "languageModels": [
                {
                    "subscriptionkey": "",
                    "appid": "",
                    "id": "",
                    "version": "",
                    "region": "",
                    "name": "",
                    "authoringkey": "",
                    "authoringRegion": ""
                }
            ],
            "knowledgebases": [
                {
                    "endpointKey": "",
                    "kbId": "",
                    "hostname": "",
                    "subscriptionKey": "",
                    "name": "",
                    "id": ""
                }
            ]
        }
    }
}
```

These cognitive models are loaded into a dictionary in Startup.cs as part of the BotSettings.cs class:

**Startup.cs**
```csharp
var settings = new BotSettings();
Configuration.Bind(settings);
```

**BotSettingsBase.cs**
```csharp
public Dictionary<string, CognitiveModelConfiguration> CognitiveModels { get; set; }
```

Then in MainDialog.cs, the CurrentUICulture local is used to select the appropriate set of cognitive models to use:

```csharp
var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
var cognitiveModels = _services.CognitiveModelSets[locale];
```

## Responses
Responses can be localized in a variety of ways. If you use resource files (.resx) the correct response will be chosen based on the CurrentUICulture. The ResponseManager class in Microsoft.Bot.Builder.Solutions can also be used to localize responses in the json format described [here]({{site.baseurl}}/skills/handbook/language-generation).

## Channel Support
The localization approach is currently supported in the following channels:
- Emulator
- Web Chat
- Direct Line
- Direct Line Speech

### Bot Framework Emulator
{:.no_toc}

To test your assistant with different locales, you follow these steps in the Bot Framework emulator:

1. Open the **Settings** tab.

    ![Emulator settings screenshot]({{site.baseurl}}/assets/images/emulator_settings.png)

2. Set your desired locale in the **Locale** field and click **Save**

    ![Emulator locale screenshot]({{site.baseurl}}/assets/images/emulator_locale.jpg)

### Web Chat
{:.no_toc}

To use this approach in webchat, you can set the locale of the activity by providing the **locale** parameter when you initialize your WebChat client, like so:

```
<script>
    window.WebChat.renderWebChat(
        {
            directLine: window.WebChat.createDirectLine({
                token: '***'
            }),
            username: 'Web Chat User',
            locale: 'en-US'
        },
        document.getElementById('webchat')
    );
</script>
```

### Direct Line & Direct Line Speech
{:.no_toc}

For Direct Line and Direct Line Speech, your client can pass the locale in the Activity.Locale property to enable localization scenarios.