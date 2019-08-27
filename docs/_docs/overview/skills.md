---
category: Overview
title: Skills
order: 2
---

# {{ page.title }}
Bot Framework Skills are re-usable conversational skill building-blocks covering conversational use-cases enabling you to add extensive functionality to a Bot within minutes. Skills include LUIS models, Dialogs and Integration code and delivered in source code form enabling you to customize and extend as required. At this time we provide Calendar, Email, To Do, Point of Interest skills and a number of other experimental skills.

A Skill is like a standard conversational bot but with the ability to be plugged in to a broader solution. This can be a complex Virtual Assistant or perhaps an Enterprise Bot seeking to stitch together multiple bots within an organization.

Apart from some minor differences that enable this special invocation pattern, a Skill looks and behaves like a regular bot. The same protocol is maintained between two bots to ensure a consistent approach. Skills for common scenarios like productivity and navigation to be used as-is or customized however a customer prefers.

>The Skill implementations currently provided are in C# only but the remote invocation nature of the Skills does enable you to invoke C# based Skills from a typescript Bot project.

## Available Skills

The following Skills are available out of the box, each of the documentation links below has the deployment steps required to deploy and configure Skills for your use.

- [Productivity - Calendar]({{site.baseurl}}/reference/skills/productivity-calendar)
- [Productivity - Email]({{site.baseurl}}/reference/skills/productivity-email)
- [Productivity - To Do]({{site.baseurl}}/reference/skills/productivity-todo)
- [Point of Interest]({{site.baseurl}}/reference/skills/pointofinterest)
- [Automotive]({{site.baseurl}}/reference/skills/automotive)
- [Experimental Skills]({{site.baseurl}}/reference/skills/experimental)
