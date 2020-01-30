---
layout: tutorial
category: Skills
subcategory: Convert a v4 Bot
language: csharp
title: Add Bot Framework Solutions packages
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}


1. Implement MVC architecture
    - If you have an existing bot using the v4 SDK, following the MVC approach from this [Bot Builder sample](https://github.com/Microsoft/BotBuilder-Samples/tree/master/samples/csharp_dotnetcore/05.multi-turn-prompt).

1. Enable the Bot Framework Solutions packages
    - Add [`Microsoft.Bot.Builder.Solutions`](https://www.nuget.org/packages/Microsoft.Bot.Builder.Solutions/) and [`Microsoft.Bot.Builder.Skills`](https://www.nuget.org/packages/Microsoft.Bot.Builder.Skills/) NuGet packages to your solution.