---
category: Solution Accelerators
subcategory: Assistants
title: Hospitality Assistant
order: 2
toc: true
---

# {{ page.title }}
{:.no_toc}

The [Hospitality Assistant sample]({{site.repo}}/tree/next/samples/assistants/hospitality-assistant) is a prototype of an assistant that helps to conceptualize and demonstrate how a virtual assistant could be used in a hospitality specific scenario. It also provides a starting point for those interested in creating an assistant customized for this scenario.

This sample works off the basis that the assistant would be integrated into a hotel room device and would help a hotel guest with anything they might usually go to the hotel concierge about. It also provides additional capabilites that might be useful for guests, such as getting the weather forecast or showing current news articles. 

The Hospitality Sample builds off of the [Virtual Assistant Template]({{site.baseurl}}/overview/virtual-assistant-template) with the addition of a [QnA Maker](https://www.qnamaker.ai/) knowledge base for answering common hotel FAQs and customized [Adaptive Cards](https://adaptivecards.io/). 

![Hospitality Sample Diagram]({{site.baseurl}}/assets/images/hospitalitysample-diagram.png)

## Supported scenarios

The majority of the skills connected to this sample are [experimental skills]({{site.baseurl}}/skills/samples/experimental), which means they are early prototypes of Skills and are likely to have rudimentary language models, limited language support and limited testing. These skills demonstrate a variety of skill concepts and provide great examples to get you started. This sample demonstrates the following scenarios:

#### Hotel FAQ
{:.no_toc}
- *Where is the gym?*
- *What time is breakfast?*
- *Do you allow pets?*

#### [Bing Search Skill]({{site.baseurl}}/skills/samples/bing-search)
{:.no_toc}
##### Search the web
{:.no_toc}
- *Tell me about the jurassic park movie*
- *Who is Bill Gates?*

#### [Event Skill]({{site.baseurl}}/skills/samples/event)
{:.no_toc}
##### Find local events
{:.no_toc}
- *What's happening nearby?* 

#### [Hospitality Skill]({{site.baseurl}}/skills/samples/hospitality)
{:.no_toc}
##### Guest reservation changes
{:.no_toc}
- *I want to extend my stay by 2 nights*
- *Can I get a late check out time?*
- *Can you check me out now*

##### Room service
{:.no_toc}
- *I want to see a room service menu*
- *Can you get me 2 croissants and a yogurt parfait?*
- *Can you bring me a toothbrush and toothpaste?*

#### [News Skill]({{site.baseurl}}/skills/samples/news)
{:.no_toc}
##### Find news articles 
{:.no_toc}
- *What's the latest news on surfing?*
- *What news is currently trending?*

#### [Restaurant Booking Skill]({{site.baseurl}}/skills/samples/restaurant-booking)
{:.no_toc}
##### Make a restaurant reservation
{:.no_toc}
- *Make a dinner reservation for tonight*

#### [Point of Interest Skill]({{site.baseurl}}/reference/skills/point-of-interest)
{:.no_toc}
##### Find points of interest nearby
{:.no_toc}
- *Find me nearby coffee shops*

#### [Weather Skill]({{site.baseurl}}/skills/samples/weather)
{:.no_toc}
##### Get the forecast
{:.no_toc}
- *What’s the weather today?* 

## Deploy
To configure this sample follow the steps below:
1. Clone the [Hospitality Assistant sample]({{site.repo}}/tree/next/samples/csharp/assistants/hospitality-assistant).
1. Follow the [Create your Virtual Assistant tutorial]({{site.baseurl}}/virtual-assistant/tutorials/create-assistant/csharp/1-intro/) to deploy your assistant. Use the sample project you cloned instead of the Virtual Assistant template to include the hospitality customizations in this project.
1. Clone the following skills from our repository:
    - [Hospitality Skill]({{site.repo}}/tree/next/skills/csharp/experimental/hospitalityskill)
    - [Event Skill]({{site.repo}}/tree/next/skills//csharp/experimental/eventskill)
    - [Point of Interest Skill]({{site.repo}}/tree/next/skills/csharp/pointofinterestskill)
    - [Weather Skill]({{site.repo}}/tree/next/skills/csharp/experimental/weatherskill)
    - [Bing Search Skill]({{site.repo}}/tree/next/skills/csharp/experimental/bingsearchskill)
    - [News Skill]({{site.repo}}/tree/next/skills/csharp/experimental/newsskill)
    - [Restaurant Booking Skill]({{site.repo}}/tree/next/skills/csharp/experimental/restaurantbookingskill)
1. [Deploy each one of these skills]({{site.baseurl}}/skills/tutorials/create-skill/csharp/4-provision-your-azure-resources/) separately, using the deployment script included in the skill directory. 
1. [Add each skill]({{site.baseurl}}/skills/handbook/add-skills-to-a-virtual-assistant/) using the botskills connect CLI tool. 

## Download transcripts

View sample conversations Hospitality Assistant solution by downloading a transcript and opening with the [Bot Framework Emulator](https://aka.ms/botframework-emulator). For more flows of specific skills see [transcripts]({{site.baseurl}}/reference/skills/transcripts).

<a class="btn btn-primary" href="{{site.baseurl}}/assets/transcripts/hospitalitysample-faqs.transcript">Frequently asked questions</a>
<a class="btn btn-primary" href="{{site.baseurl}}/assets/transcripts/hospitalitysample-localinfo.transcript">Local info</a>
<a class="btn btn-primary" href="{{site.baseurl}}/assets/transcripts/hospitalitysample-reservationchanges.transcript">Reservation changes</a>
<a class="btn btn-primary" href="{{site.baseurl}}/assets/transcripts/hospitalitysample-roomservices.transcript">Room services</a>
