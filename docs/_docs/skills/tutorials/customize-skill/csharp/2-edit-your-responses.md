---
layout: tutorial
category: Skills
subcategory: Customize
language: C#
title: Edit responses
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Edit default responses
Edit the MainResponses.lg and SharedResponses.lg files in the Responses folder to modify the default responses used by the template.

## Add additional responses
If you wish to add additional responses, add an additional LG file to the directory and populate as required. See [this reference](https://github.com/microsoft/botbuilder-dotnet/tree/master/doc/LanguageGeneration) for more information on Language Generation.

Within `Startup.cs` in your project root directory add the newly created LG file to the templateFiles collection.

```csharp
// Configure localized responses
var localizedTemplates = new Dictionary<string, List<string>>();
var templateFiles = new List<string>() { "MainResponses", "SampleResponses" };
var supportedLocales = new List<string>() { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" };

```

### Learn More
For more information, refer to the [Skill Responses reference]({{site.baseurl}}/skills/handbook/language-generation).
