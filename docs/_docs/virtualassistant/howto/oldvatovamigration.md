---
category: Virtual Assistant
subcategory: How-to
title: Migrate from Virtual Assistant solution (v0.3)
description: Guidance on how to move from the original Virtual Assistant solution to the new Template
order: 7
---

# {{ page.title }}
{:.no_toc}

## In this how-to
{:.no_toc}

* 
{:toc}

## Prerequisites

Learn how to [migrate an Enterprise Template based bot to the Virtual Assistant Template]({{site.baseurl}}/howto/virtual-assistant/ettovamigration). After doing so you will have a Virtual Assistant project ready to add the Productivity & Point of Interest SKills.

## What happened to the Virtual Assistant Skills?

The Virtual Assistant (Beta Release 0.3) solution was delivered with multiple preview Skills to support productivity & point of interest scenarios. These have now been made available as [**Bot Framework Skills**]({{site.baseurl}}/overview/skills), reusable conversational skill that can be added to an existing bot. Developers can add and remove Skills with one command that incoporates all language models and configuration changes. Skills are themselves Bots, invoked remotely and a Skill developer template (.NET, TS) is available to facilitate creation of new Skills.

## Add the Skills to your assistant

The Skills previously part of the Virtual Assistant solution are relocated to the [skills directory](https://github.com/microsoft/botframework-solutions/tree/master/skills/src/csharp). After deploying your selected Skills, continue to [add them to your Virtual Assistant]({{site.baseurl}}/howto/skills/addingskills).