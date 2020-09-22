---
layout: tutorial
category: Skills
subcategory: Customize
language: csharp
title: Edit responses
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Edit your responses

Each dialog within your skill contains a set of responses stored in supporting Language Generation (`.lg`) files. You can edit the responses directly in the file to modify how your skill responds. Adjust some of the responses to suit your Assistant / Skill's personality.

This approach supports multi-lingual responses by providing alternate .lg files for different languages (e.g. "MainResponses.de-de.lg"). By expanding MainResponses.lg or SampleResponses.lg in Visual Studio you can see the accompany multi-lingual response files.

Edit the MainResponses.lg and SampleResponses.lg files in the Responses folder to modify the default responses used by the template.

## Add additional responses
If you wish to add additional responses, add an additional LG file to the directory and populate as required. See [this reference](https://docs.microsoft.com/en-us/composer/concept-language-generation) for more information on Language Generation.

Within `Startup.cs` in your project root directory add the newly created LG file to the templateFiles collection.

```csharp
// Configure localized responses
var localizedTemplates = new Dictionary<string, List<string>>();
var templateFiles = new List<string>() { "MainResponses", "SampleResponses" };
var supportedLocales = new List<string>() { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" };

```

## Multiple Responses

Language Generation enables multiple responses to be provided for each response type enabling the LG engine to randomly select a response at runtime. Providing multiple options enables a more natural experience for your users and the provided LG files provide a variety of options that you can customize.

Learn more about the Language Generation template syntax [here](https://docs.microsoft.com/en-us/azure/bot-service/file-format/bot-builder-lg-file-format?view=azure-bot-service-4.0).
