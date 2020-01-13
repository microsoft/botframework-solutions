---
layout: tutorial
category: Skills
subcategory: Create
language: csharp
title: Intro
order: 1
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

### Purpose

Install Bot Framework development prerequisites and create a Skill using the Bot Framework Skill Template.

### Prerequisites

- Azure Subscription
- LUIS Authoring Key
    - Option 1: Use a LUIS starter key
        - Go to the LUIS portal for your desired region.
        - Once signed in, select your initials in the right-hand corner, then select **Settings**.
        - Under **Starter_Key**, copy the **Primary Key**.
    - Option 2: Provision a LUIS authoring resource in Azure
        - In the Azure Portal, create a new **Language Understanding** resource.
        - Under **Create options**, select **Authoring**.
        - Select a **Resource group**, Authoring location, and Authoring pricing tier. 
        - Click **Create**.
        - Go to the resource and select **Keys** in the menu.
        - Copy one of the available keys.
    
    > Note that LUIS authoring keys for a given region are not valid for models hosted in another region. Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation for more information.

### Time to Complete

20 minutes

### Scenario

A Bot Framework Skill app (in C#) that greets a new user.
