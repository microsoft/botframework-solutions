---
category: Archive
subcategory: Samples
language: Experimental Skills
title: Event Skill
description: Event Skill provides ability to search for events using Eventbrite.
order: 4
toc: true
---

# {{ page.title }}
{:.no_toc}

The [Event Skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/eventskill) provides a simple skill that integrates with [Eventbrite](https://www.eventbrite.com/platform/) to show information about events happening in the specified area.

This skill currently supports one scenario to get local event information.

![Event Example]({{site.baseurl}}/assets/images/skills-event-transcript.png)

## Configuration
{:.no_toc}

1. Get your own [Eventbrite API Key](https://www.eventbrite.com/platform/api-keys).
1. Provide this value in your `appsettings.json` file.

```
"eventbriteKey":  "YOUR_EVENTBRITE_API_KEY"
```