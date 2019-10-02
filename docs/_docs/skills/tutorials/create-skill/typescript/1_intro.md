---
layout: tutorial
category: Skills
subcategory: Create a Skill
language: TypeScript
title: Intro
order: 1
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## Intro

### Purpose

Install Bot Framework development prerequisites and create a Skill using the Bot Framework Skill Template.

### Prerequisites

If you haven't [created a Virtual Assistant]({{site.baseurl}}/tutorials/typescript/create-assistant/1_intro/), [download and install]({{site.baseurl}}/tutorials/typescript/create-assistant/2_download_and_install/) the Bot Framework development prerequisites.

- Retrieve your LUIS Authoring Key
  - Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation page for the correct LUIS portal for the region you plan to deploy to. Note that www.luis.ai refers to the US region and an authoring key retrieved from this portal will not work within a europe deployment. 
  - Once signed in replace your name in the top right hand corner.
  - Choose Settings and make a note of the Authoring Key for the next step.

### Time to Complete

20 minutes

### Scenario

A Bot Framework Skill app (in TypeScript) that greets a new user.