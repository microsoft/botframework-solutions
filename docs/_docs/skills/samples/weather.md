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

The [Weather skill](https://github.com/microsoft/botframework-skills/tree/master/skills/csharp/experimental/weatherskill) provides a basic Skill that integrates with [Azure Maps](https://azure.microsoft.com/en-us/services/azure-maps/) to demonstrate how a weather experience can be integrated into a Virtual Assistant.

## Deployment
{:.no_toc}
Learn how to [provision your Azure resources]({{site.baseurl}}/skills/tutorials/create-skill/csharp/4-provision-your-azure-resources/) in the Create a Skill tutorial.

### Supported content providers
{:.no_toc}

> **Mandatory**: [Azure Maps](https://azure.microsoft.com/en-us/services/azure-maps/) is supported for getting detailed weather forecast such as temperature, humidity, wind of a selected location.

## Configuration
{:.no_toc}

1. Create your own Azure Maps account. Get your Primary Key.
1. Provide this value in your `appsettings.json` file.

```
"WeatherApiKey": "{YOUR_AzureMaps_KEY}"
```
