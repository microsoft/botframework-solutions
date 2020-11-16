---
layout: tutorial
category: Virtual Assistant
subcategory: Customize
language: csharp
title: Edit your responses
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Edit your responses

Each dialog within your assistant contains a set of responses stored in supporting Language Generation (`.lg`) files. You can edit the responses directly in the file to modify how your assistant responds. Adjust some of the responses to suit your Assistant's personality.

This approach supports multi-lingual responses by providing alternate `.lg` files for different languages (e.g. _MainResponses.de-de.lg_). By expanding _MainResponses.lg_ or _OnboardingResponses.lg_ in Visual Studio you can see the accompany multi-lingual response files.

## Add additional responses
If you wish to add additional responses, add an additional LG file to the directory and populate as required. See [this reference](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-concept-language-generation?view=azure-bot-service-4.0&tabs=csharp) for more information on Language Generation.

Within `AllResponses.lg` in your `Responses` directory, add the newly created LG file.

```markdown
[Main](./MainResponses.lg)
[Onboarding](./OnboardingResponses.lg)
```

Your LG file will be added automatically in the `templateFile` variable of the `Startup.cs`.
```csharp
// Configure localized responses
var localizedTemplates = new Dictionary<string, string>();
var templateFile = "AllResponses";
var supportedLocales = new List<string>() { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" };
```

## Multiple Responses

Language Generation enables multiple responses to be provided for each response type enabling the LG engine to randomly select a response at runtime. Providing multiple options enables a more natural experience for your users and the provided LG files provide a variety of options that you can customize.

## Randomization

Within `MainResponses` we provide an example of occasionally using the Users name in responses, being overly familiar can be an issue hence the selective use here whereby the Name will be used at random.

```markdown
# ConfusedMessage
- I'm sorry, I didn’t understand that. Can you give me some more information?
- Sorry, I didn't get that. Can you tell me more?
- Sorry ${RandomName()}, I didn't get that. Can you tell me more?
- Apologies, I didn't quite understand. Can you give me more information?

# RandomName 
- IF: ${Name && rand(0, 1000) > 500}
    - ${concat(' ', Name)}
- ELSE:
    - 
```

Learn more about the Language Generation template syntax [here](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-concept-language-generation?view=azure-bot-service-4.0&tabs=csharp).