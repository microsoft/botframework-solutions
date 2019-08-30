---
category: Reference
subcategory: Samples
title: Hospitality Assistant
description: Virtual Assistant sample made for a hospitality scenario.
order: 2
---

# {{ page.title }}
{:.no_toc}

## In this reference
{:.no_toc}

* 
{:toc}

## Overview
The [Hospitality Sample Virtual Assistant]({{site.repo}}/tree/master/samples/assistants/HospitalitySample) is a prototype of an assistant that helps to conceptualize and demonstrate how a virtual assistant could be used in a hospitality specific scenario. It also provides a starting point for those interested in creating an assistant customized for this scenario.

This sample works off the basis that the assistant would be integrated into a hotel room device and would help a hotel guest with anything they might usually go to the hotel concierge about. It also provides additional capabilites that might be useful for guests, such as getting the weather forecast or showing current news articles. 

The Hospitality Sample builds off of the [Virtual Assistant Template]({{site.baseurl}}/overview/virtualassistant) with the addition of a [QnA Maker](https://www.qnamaker.ai/) knowledge base for answering common hotel FAQs and [Adaptive Cards](https://adaptivecards.io/) customized for hospitality. It also connects 7 different skills, which are [Hospitality]({{site.baseurl}}/reference/skills/experimental/#hospitality-skill), [Event]({{site.baseurl}}/reference/skills/experimental/#event-skill), [Point of Interest]({{site.baseurl}}/reference/skills/pointofinterest), [Weather]({{site.baseurl}}/reference/skills/experimental/#weather-skill), [Bing Search]({{site.baseurl}}/reference/skills/experimental/#bing-search-skill), [News]({{site.baseurl}}/reference/skills/experimental/#news-skill), and [Restaurant Booking]({{site.baseurl}}/reference/skills/experimental/#restaurant-booking-skill).

![Hospitality Sample Diagram]({{site.baseurl}}/assets/images/hospitalitysample-diagram.png)

The majority of the skills connected to this sample are [experimental skills]({{site.baseurl}}/reference/skills/experimental), which means they are early prototypes of Skills and are likely to have rudimentary language models, limited language support and limited testing. These skills demonstrate a variety of skill concepts and provide great examples to get you started.

## Sample Configuration
To configure this sample follow the steps below:
1. Clone the [Hospitality Sample from our repository]({{site.repo}}/tree/master/samples/assistants/HospitalitySample).
2. Follow the [Create your Virtual Assistant tutorial]({{site.baseurl}}/tutorials/csharp/create-assistant/1_intro/) to deploy your assistant. Use the sample project you cloned instead of the Virtual Assistant template to include the hospitality customizations in this project.
3. Clone the following skills from our repository:
    - [Hospitality Skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/hospitalityskill)
    - [Event Skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/eventskill)
    - [Point of Interest Skill]({{site.repo}}/tree/master/skills/src/csharp/pointofinterestskill/pointofinterestskill)
    - [Weather Skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/weatherskill)
    - [Bing Search Skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/bingsearchskill/bingsearchskill)
    - [News Skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/newsskill)
    - [Restaurant Booking Skill]({{site.repo}}/tree/master/skills/src/csharp/experimental/restaurantbooking)
4. [Deploy each one of these skills]({{site.baseurl}}/tutorials/csharp/create-skill/4_provision_your_azure_resources/) separately, using the deployment script included in the skill directory. 
4. [Add each skill]({{site.baseurl}}/howto/skills/addingskills/) using the botskills connect CLI tool. 

## Supported Scenarios
This sample demonstrates the following scenarios:
- Answer hotel FAQs ([QnA Maker](https://www.qnamaker.ai/) knowledge base)
    - *Where is the gym?*
    - *What time is breakfast?*
    - *Do you allow pets?*
- Guest reservation changes ([Hospitality Skill]({{site.baseurl}}/reference/skills/experimental/#hospitality-skill))
    - *I want to extend my stay by 2 nights*
    - *Can I get a late check out time?*
    - *Can you check me out now*
- Room services ([Hospitality Skill]({{site.baseurl}}/reference/skills/experimental/#hospitality-skill))
    - *I want to see a room service menu*
    - *Can you get me 2 croissants and a yogurt parfait?*
    - *Can you bring me a toothbrush and toothpaste?*
- Get local area information ([Event]({{site.baseurl}}/reference/skills/experimental/#event-skill) and [Point of Interest]({{site.baseurl}}/reference/skills/pointofinterest) skills)
    - *What's happening nearby?* 
    - *Find me nearby coffee shops*
- Make a restaurant reservation ([Restaurant Booking Skill]({{site.baseurl}}/reference/skills/experimental/#restaurant-booking-skill))
    - *Make a dinner reservation for tonight*
- Weather forecast ([Weather Skill]({{site.baseurl}}/reference/skills/experimental/#weather-skill))
    - *What's the weather today?*
- Find news articles ([News Skill]({{site.baseurl}}/reference/skills/experimental/#news-skill))
    - *What's the latest news on surfing?*
    - *What news is currently trending?*
- Search the web ([Bing Search Skill]({{site.baseurl}}/reference/skills/experimental/#bing-search-skill))
    - *Tell me about the jurassic park movie*
    - *Who is Bill Gates?*

For a more in-depth explanation of the scenarios supported by each skill check out the [experimental skills documentation]({{site.baseurl}}/reference/skills/experimental) and [Point of Interest Skill documentation]({{site.baseurl}}/reference/skills/pointofinterest).

## Transcripts
Review sample conversational flows for the Hospitality Sample Assistant by downloading the following transcripts and opening with the [Bot Framework Emulator](https://aka.ms/botframework-emulator). For more flows of specific skills see [skills transcripts]({{site.baseurl}}/reference/skills/transcripts).

**Hotel FAQs**: [Download]({{site.baseurl}}/assets/transcripts/hospitalitysample-faqs.transcript)

**Reservation changes**: [Download]({{site.baseurl}}/assets/transcripts/hospitalitysample-reservationchanges.transcript)

**Room services**: [Download]({{site.baseurl}}/assets/transcripts/hospitalitysample-roomservices.transcript)

**Local information**: [Download]({{site.baseurl}}/assets/transcripts/hospitalitysample-localinfo.transcript)

