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
To capture the user's locale, the Virtual Assistant uses the `SetLocaleMiddleware`. For each message that comes in from the user, the `CurrentUICulture` is set equal to the Activity's locale property. If Activity.Locale is not available on the activity, the `defaultLocale` property from `cognitivemodels.json` is used instead.

## Cognitive Models
Each cognitive model used by the assistant (i.e. LUIS, QnA Maker, Dispatch) should be deployed in each language you want to support. The configuration for these models should be included in the `cognitiveModels` collection in the `cognitivemodels.json` file.

**Example cognitivemodels.json**
```json
{
    "defaultLocale": "en-us",
    "cognitiveModels": {
        "en-us": {
            "dispatchModel": {
                "appid": "",
                "authoringkey": "",
                "authoringRegion": "",
                "name": "",
                "region": "",
                "subscriptionkey": "",
                "type": "dispatch"
            },
            "languageModels": [
                {
                    "appid": "",
                    "authoringkey": "",
                    "authoringRegion": "",
                    "id": "",
                    "name": "",
                    "region": "",
                    "subscriptionkey": "",
                    "version": ""
                }
            ],
            "knowledgebases": [
                {
                    "endpointKey": "",
                    "hostname": "",
                    "id": "",
                    "kbId": "",
                    "name": "",
                    "subscriptionKey": ""
                }
            ]
        },
        "es-es": {
            "dispatchModel": {
                "appid": "",
                "authoringkey": "",
                "authoringRegion": "",
                "name": "",
                "region": "",
                "subscriptionkey": "",
                "type": "dispatch"
            },
            "languageModels": [
                {
                    "appid": "",
                    "authoringkey": "",
                    "authoringRegion": "",
                    "id": "",
                    "name": "",
                    "region": "",
                    "subscriptionkey": "",
                    "version": ""
                }
            ],
            "knowledgebases": [
                {
                    "endpointKey": "",
                    "hostname": "",
                    "id": "",
                    "kbId": "",
                    "name": "",
                    "subscriptionKey": ""
                }
            ]
        }
    }
}
```

These cognitive models are loaded into a dictionary in `Startup.cs` as part of the `BotSettings.cs` class:

**Startup.cs**
```csharp
var settings = new BotSettings();
Configuration.Bind(settings);
```

**BotSettingsBase.cs**
```csharp
public Dictionary<string, CognitiveModelConfiguration> CognitiveModels { get; set; } = new Dictionary<string, CognitiveModelConfiguration>();
```

Then in `BotServices.cs`, the `CurrentUICulture` local is used to select the appropriate set of cognitive models to use in the `GetCognitiveModels()` method:

```csharp
public CognitiveModelSet GetCognitiveModels()
{
    // Get cognitive models for locale
    var locale = CultureInfo.CurrentUICulture.Name.ToLower();

    var cognitiveModel = CognitiveModelSets.ContainsKey(locale)
        ? CognitiveModelSets[locale]
        : CognitiveModelSets.Where(key => key.Key.StartsWith(locale.Substring(0, 2))).FirstOrDefault().Value
        ?? throw new Exception($"There's no matching locale for '{locale}' or its root language '{locale.Substring(0, 2)}'. " +
                                "Please review your available locales in your cognitivemodels.json file.");

    return cognitiveModel;
}
```

## Responses

Responses from your assistant are generated through use of [Language Generation](https://docs.microsoft.com/en-us/composer/concept-language-generation) and the `.lg` files within the `Responses` folder of your assistant. We provide a number of different language variations out of the box to get you started.

The provided [LocaleTemplateManager](https://github.com/microsoft/botframework-solutions/blob/master/sdk/csharp/libraries/microsoft.bot.solutions/Responses/LocaleTemplateManager.cs) will identify the right response based on the `CurrentUICulture.

> A fallback policy is also followed to enable a specific locale, e.g. `es-mx`, to fallback to `es-es` if a specific set of `es-mx` responses are not available.

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

1. Set your desired locale in the **Locale** field and click **Save**

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