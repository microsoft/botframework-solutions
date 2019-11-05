---
category: Skills
subcategory: Samples
language: Experimental Skills
title: Bing Search Skill
description: Bing Search Skill provides the ability to use Bing to provide answers to common search questions.
order: 3
toc: true
---

# {{ page.title }}
{:.no_toc}

The [Bing Search Skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/bingsearchskill) provides a simple Skill that integrates with the [Bing Search Cognitive Service](https://azure.microsoft.com/en-us/services/cognitive-services/bing-web-search-api/).

This skill has a very limited LUIS model (available in English, French, Italian, German, Spanish and Chinese) and demonstates three simple scenarios:

- Celebrity Information: *Who is Bill Gates?*
- Q&A: *what's the population of China?*
- Movie Information: *Tell me about the jurassic park movie*

![Search Example]({{site.baseurl}}/assets/images/skills-experimental-bingsearch.png)

## Configuration
{:.no_toc}

1. Get your own [Bing Search Cognitive Services Key](https://azure.microsoft.com/en-us/services/cognitive-services/bing-web-search-api/).
1. Get your own [Project Answer Search Key](https://labs.cognitive.microsoft.com/en-us/project-answer-search).
1. Provide these values in your `appsettings.json` file.

```
"BingSearchKey": "{YOUR_BING_SEARCH_COGNITIVE_SERVICES_KEY}",
"BingAnswerSearchKey": "{YOUR_PROJECT_ANSWER_SEARCH_KEY}"
```