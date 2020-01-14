---
layout: tutorial
category: Skills
subcategory: Extend a v4 Bot
language: typescript
title: Skill configuration
order: 3
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

The 'botbuilder-skills' package provides a `ISkillManifest` interface that describes a Skill. Your bot should maintain a collection of registered Skills typically serialized into a `JSON` configuration file. The Virtual Assistant template uses a `skills.json` file for this purpose that can be found in the `src` directory.

That file must have the following structure:

```json
{
  "skills": []
}
```

As part of your Configuration processing you should construct a collection of registered Skills by deserializing this file, for example:

```typescript
import { skills as skillsRaw } from './skills.json';
const skills: ISkillManifest[] = skillsRaw;
```

> NOTE: The `botbuilder-skills` package also provides a `IBotSettings` interface that can be used to storage the keys/secrets of the services that will be used to connect services to the bot.
