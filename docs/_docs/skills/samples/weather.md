---
category: Skills
subcategory: Samples
language: experimental_skills
title: Weather Skill
description: Weather Skill provides the ability to look up the weather for a location.
order: 11
toc: true
---

# {{ page.title }}
{:.no_toc}

The [Weather skill]({{site.repo}}/tree/master/skills/csharp/experimental/weatherskill) provides a basic Skill that integrates with [AccuWeather](https://developer.accuweather.com) to demonstrate how a weather experience can be integrated into a Virtual Assistant.

## Configuration
{:.no_toc}

1. Get your own API Key when by following the instructions on [AccuWeather Getting Started](https://developer.accuweather.com/getting-started).
1. Provide this value in your `appsettings.json` file.

```
"WeatherApiKey": "{YOUR_ACCUWEATHER_API_KEY}"
```