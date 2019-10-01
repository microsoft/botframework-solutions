---
layout: tutorial
category: Virtual Assistant
subcategory: Tutorials
subsubcategory: Create a Virtual Assistant
language: C#
title: Intro
order: 1
---

# Tutorial: {{page.subsubcategory}} ({{page.language}})
## Intro

### Purpose
Install Bot Framework development prerequisites and create your first Virtual Assistant.

### Prerequisites
- Azure Subscription
- LUIS Authoring Key
    - Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation page for the correct LUIS portal for the region you plan to deploy to. Note that www.luis.ai refers to the US region and an authoring key retrieved from this portal will not work within a Europe deployment.
    - Once signed in, select your initials in the uppermost right-hand corner of the page to view the profile menu.
    - Select Settings and make a note of the Authoring Key for the next step.

### Time To Complete
20 minutes

### Scenario
A Virtual Assistant app (in C#) that greets a new user and handles basic conversational intents.