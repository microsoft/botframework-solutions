---
layout: tutorial
category: Skills
subcategory: Extend a v4 Bot
title: Skill configuration
language: C#
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

The `Microsoft.Bot.Builder.Skills` package provides a `SkillManifest` type that describes a Skill. Your bot should maintain a collection of registered Skills typically serialized into a JSON configuration file. The Virtual Assistant template uses a `skills.json` file for this purpose.

As part of your Configuration processing you should construct a collection of registered Skills by deserializing this file, for example:

```csharp
public List<SkillManifest> Skills { get; set; }
```