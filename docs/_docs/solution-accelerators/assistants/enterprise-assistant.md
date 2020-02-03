---
category: Solution Accelerators
subcategory: Assistants
title: Enterprise Assistant
order: 1
toc: true
---
# {{ page.title }}
{:.no_toc}
Many organizations are looking to provide a centralized conversational experience across many canvases for employees. This concept allows for a consolidation of many disparate bots across the organization to a more centralized solution where a master bot handles finding the right bot to handle the conversation, thus avoiding bot explosion through parent bot/skills approach. This, in turn, gets the user productive quicker and allows for a true Enterprise Virtual Assistant Experience. 

The [Enterprise Assistant sample]({{site.repo}}/tree/master/samples/csharp/assistants/enterprise-assistant) is an example of a Virtual Assistant that helps conceptualize and demonstrate how an assistant could be used in common enterprise scenarios. It also provides a starting point for those interested in creating an assistant customized for this scenario. 

![Enterprise Assistant Overview Drawing]({{site.baseurl}}/assets/images/EnterpriseAssistantSampleOverview.PNG)

This sample works off the basis that the assistant would be provided through common employee channels such as Microsoft Teams, a mobile application, and Web Chat to help improve employee productivity, but also assist them in getting work tasks completed such as opening an IT Service Management (ITSM) ticket. It also provides additional capabilities that might be useful for employees, like getting the weather forecast or showing current news articles. 

The Enterprise Assistant Sample is based on the [Virtual Assistant Template]({{site.baseurl}}/overview/virtual-assistant-template), with the addition of a [QnA Maker knowledge base](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/concepts/knowledge-base) for answering common enterprise FAQs (such as Benefits and HR Information) and customized Adaptive Cards.  It also connects 7 different Skills; which are [Calendar]({{site.baseurl}}/skills/samples/calendar), [Email]({{site.baseurl}}/skills/samples/email), and [To Do]({{site.baseurl}}/skills/samples/to-do)  along with the experimental skills of [Weather]({{site.baseurl}}/skills/samples/weather), [News]({{site.baseurl}}/skills/samples/news), [Phone]({{site.baseurl}}/skills/samples/phone) and [ITSM]({{site.baseurl}}/skills/samples/itsm). 

In many cases, you can leverage [Azure Active Directory (AAD)](https://azure.microsoft.com/en-us/services/active-directory/) for single sign-on (SSO), though this may be limited by the channel itself and your specific requirements. 


## Proactive notifications

The Enterprise Assistant sample includes [proactive notifications]({{site.baseurl}}/solution-accelerators/tutorials/enable-proactive-notifications/1-intro), enabling scenarios such as:

- Send notifications to your users that the Enterprise Assistant would like to start a conversation, thus allowing the user to indicate when they are ready to have this discussion 
    - e.g., a user receives a notification "your training is due", allowing them to initiate the conversation about what training is required) 

- Initiate a proactive dialog with your users through an open channel such as Microsoft Teams 
    - e.g., "Benefits enrollment just opened; would you like to know more about benefits?"


## Supported scenarios

The majority of the skills connected to this sample are [experimental skills]({{site.baseurl}}/overview/skills/#experimental-skills), which means they are early prototypes of Skills and are likely to have rudimentary language models, limited language support and limited testing. These skills demonstrate a variety of skill concepts and provide great examples to get you started. This sample demonstrates the following scenarios:

#### HR FAQ
{:.no_toc}
- *I need life insurance* 
- *How do I sign up for benefits?* 
- *What is HSA?* 

#### [Calendar Skill]({{site.baseurl}}/skills/samples/calendar) 
{:.no_toc}
##### Connect to a meeting 
{:.no_toc}
- *Connect me to conference call* 
- *Connect me with my 2 o’clock meeting* 

##### Create a meeting 
{:.no_toc}
- *Create a meeting tomorrow at 9 AM with Lucy Chen* 
- *Put anniversary on my calendar* 

##### Delete a meeting 
{:.no_toc}
- *Cancel my meeting at 3 PM today* 
- *Drop my appointment for Monday* 

##### Find a meeting 
{:.no_toc}
- *Do I have any appointments today?* 
- *Get to my next event* 

#### [Email]({{site.baseurl}}/skills/samples/email)
{:.no_toc}
##### Send an email
{:.no_toc}
- *Send an email to John Smith*
- *What are my latest messages?* 

#### [To Do Skill]({{site.baseurl}}/skills/samples/to-do)
{:.no_toc}
##### Add a task 
{:.no_toc}
- *Add some items to the shopping notes* 
- *Put milk on my grocery list* 
- *Create task to meet Leon after 5:00 PM* 

#### [Weather Skill]({{site.baseurl}}/skills/samples/weather)
{:.no_toc}
##### Get the forecast
{:.no_toc}
- *What’s the weather today?* 

#### [News Skill]({{site.baseurl}}/skills/samples/news)
{:.no_toc}
##### Find news articles 
{:.no_toc}
- *What’s the latest news on technology?* 
- *What news is currently trending?* 

#### [Phone Skill]({{site.baseurl}}/skills/samples/phone)
{:.no_toc}
##### Make an outgoing call
{:.no_toc}
- *Call Sanjay Narthwani* 
- *Call 867 5309* 
- *Make a call* 

#### [IT Service Management (ITSM) Skill]({{site.baseurl}}/skills/samples/itsm)
{:.no_toc}
##### Create a ticket 
{:.no_toc}
- *Create a ticket for my broken laptop* 

##### Show a ticket 
{:.no_toc}
- *What’s the status of my incident?* 

##### Update a ticket
{:.no_toc}
- *Change ticket’s urgency to high* 

##### Close a ticket
{:.no_toc}
- *Close my ticket* 

## Deploy

An automated deployment (including proactive notifications) will be available soon.

## Download transcripts

Sample transcripts for the Enterprise Assistant will be available soon.