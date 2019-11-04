---
layout: tutorial
category: Virtual Assistant
subcategory: Create
language: TypeScript
title: Intro
order: 1
---

# Tutorial: {{page.subcategory}} ({{page.language}})
## {{ page.title }}

### Purpose

Install Bot Framework development prerequisites and create your first Virtual Assistant.

### Prerequisites

[Download and install](#download-and-install) the Bot Framework development prerequisites.

* Retrieve your LUIS Authoring Key
  - Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation page for the correct LUIS portal for the region you plan to deploy to. Note that www.luis.ai refers to the US region and an authoring key retrieved from this portal will not work within a Europe deployment. 
  - Once signed in, select your initials in the uppermost right-hand corner of the page to view the profile menu.
  - Select Settings and make a note of the Authoring Key for the next step.

### Time to Complete

10 minutes

### Scenario

A Virtual Assistant app (in TypeScript) that greets a new user.