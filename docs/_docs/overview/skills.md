---
category: Overview
title: What is a Bot Framework Skill?
order: 4
toc: true
---

# {{ page.title }}
{:.no_toc}

Bot Framework Skills are re-usable conversational skill building-blocks covering conversational use-cases enabling you to add extensive functionality to a Bot within minutes. Skills include [Language Understanding](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/what-is-luis) (LUIS) models, dialogs and integration code, delivered as source code enabling you to customize and extend as required. At this time we provide Calendar, Email, To Do, Point of Interest skills and a number of other experimental skills.

A Skill is like a standard conversational bot but with the ability to be plugged in to a broader solution. This can be a complex Virtual Assistant or perhaps an Enterprise Bot seeking to stitch together multiple bots within an organization.

Apart from some minor differences that enable this special invocation pattern, a Skill looks and behaves like a regular bot. The same protocol is maintained between two bots to ensure a consistent approach. Skills for common scenarios like productivity and navigation can be used as-is or customized however a customer prefers.

>The Skill implementations currently provided are in C# only but the remote invocation nature of the Skills does enable you to invoke C# based Skills from a TypeScript Bot project.

## Available Skill samples

The following Skill samples are available out of the box, each with deployment steps required to deploy and configure Skills for your use.

<div class="card-deck">
    {% include button.html params=site.data.button.calendar_skill %}
    {% include button.html params=site.data.button.email_skill %}
</div>
<br/>
<div class="card-deck">
    {% include button.html params=site.data.button.todo_skill %}
    {% include button.html params=site.data.button.poi_skill %}
</div>

### Experimental Skills

These experimental Bot Framework Skills are early prototypes to help bring Skill concepts to life for demonstrations and proof-of-concepts.
By their very nature these Skills are not complete, with only English support. If you have any feedback on these Skills, please [open a new issue](https://github.com/microsoft/botframework-skills/issues/new/choose) on the Bot Framework Skills repository.

- [Automotive]({{site.baseurl}}/skills/samples/automotive)
- [Bing Search]({{site.baseurl}}/skills/samples/bing-search)
- [Hospitality]({{site.baseurl}}/skills/samples/hospitality/)
- [IT Service Management (ITSM)]({{site.baseurl}}/skills/samples/itsm)
- [Music]({{site.baseurl}}/skills/samples/music)
- [News]({{site.baseurl}}/skills/samples/news)
- [Phone]({{site.baseurl}}/skills/samples/phone)
- [Restaurant Booking]({{site.baseurl}}/skills/samples/restaurant-booking)
- [Weather]({{site.baseurl}}/skills/samples/weather)

## Next steps

<div class="card-deck">
    {% include button.html params=site.data.button.create_skill_cs %}
    {% include button.html params=site.data.button.create_skill_ts %}
</div>
