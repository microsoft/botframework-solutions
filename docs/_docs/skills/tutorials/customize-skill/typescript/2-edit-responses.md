---
layout: tutorial
category: Skills
subcategory: Customize
language: typescript
title: Edit your responses
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Edit your responses
Each dialog within your skill contains a set of responses stored in supporting Language Generation (`.lg`) files. You can edit the responses directly in the file to modify how your skill responds. Adjust some of the responses to suit your Skill's personality.

This approach supports multi-lingual responses by providing alternate `.lg` files for different languages (e.g. _MainResponses.de-de.lg_). By expanding _MainResponses.lg_ or _SampleResponses.lg_ in Visual Studio you can see the accompany multi-lingual response files.

## Add additional responses
If you wish to add additional responses, add an additional LG file to the directory and populate as required. See [this reference](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-concept-language-generation?view=azure-bot-service-4.0&tabs=javascript) for more information on Language Generation.

Within `AllResponses.lg` in your `Responses` directory, add the newly created LG file.
```markdown
[Main](./MainResponses.lg)
[Sample](./SampleResponses.lg)
```

Your LG file will be added automatically in the `templateFile` variable of the `index.ts`.
```typescript
// Configure localized responses
const localizedTemplates: Map<string, string> = new Map<string, string>();
const templateFile = 'AllResponses';
const supportedLocales: string[] = ['en-us', 'de-de', 'es-es', 'fr-fr', 'it-it', 'zh-cn'];
```

## Multiple Responses

Language Generation enables multiple responses to be provided for each response type enabling the LG engine to randomly select a response at runtime. Providing multiple options enables a more natural experience for your users and the provided LG files provide a variety of options that you can customize.

### Learn More
For more information, refer to the [Skill Responses reference]({{site.baseurl}}/skills/handbook/language-generation).
