---
category: Skills
subcategory: Samples
language: Experimental Skills
title: News Skill
description: News Skill provides the ability to find and review news articles.
order: 8
toc: true
---

# {{ page.title }}
{:.no_toc}

The [News skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/newsskill) provides a simple Skill that integrates with the Bing News Cognitive Service to demonstrate how a news experience can be integrated into a Virtual Assistant.

Once deployed create a [Bing News Cognitive Services Key](https://azure.microsoft.com/en-us/services/cognitive-services/bing-news-search-api/) and update the appropriate configuration within appSettings.config.

This skill supports the following scenarios:
- Find articles: *Find me news about sports*
- Trending articles: *What news is trending now?*
- Show favorite topic: *Find news for me*

![News Example]({{site.baseurl}}/assets/images/skills-news-transcript.png)

## Configuration
{:.no_toc}

1. Get your own [Bing News Cognitive Services Key](https://azure.microsoft.com/en-us/services/cognitive-services/bing-news-search-api/)
1. Provide this value in your `appsettings.json` file.

```
"BingNewsKey": "{YOUR_BING_NEWS_COGNITIVE_SERVICES_KEY}"
```