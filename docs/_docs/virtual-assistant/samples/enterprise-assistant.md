---
category: Virtual Assistant
subcategory: Samples
title: Enterprise Assistant
order: 1
toc: true
---
# {{ page.title }}
{:.no_toc}
Many organizations are looking to provide a centralized conversational experience across many canvases for employees. This concept allows for a consolidation of many disparate bots across the organization to a more centralized solution where a master bot handles finding the right bot to handle the conversation, thus avoiding bot explosion through parent bot/skills approach. This, in turn, gets the user productive quicker and allows for a true Enterprise Virtual Assistant Experience. 

The [Enterprise Assistant sample]({{site.repo}}/tree/master/samples/csharp/assistants/enterprise-assistant) is an example of a Virtual Assistant that helps conceptualize and demonstrate how an assistant could be used in common enterprise scenarios. It also provides a starting point for those interested in creating an assistant customized for this scenario. 

This sample works off the basis that the assistant would be provided through common employee channels such as Microsoft Teams, a mobile application, and Web Chat to help improve employee productivity, but also assist them in getting work tasks completed such as opening an IT Service Management (ITSM) ticket. It also provides additional capabilities that might be useful for employees, like getting the weather forecast or showing current news articles. 

The Enterprise Assistant Sample is based on the [Virtual Assistant Template]({{site.baseurl}}/overview/virtual-assistant-template), with the addition of a [QnA Maker knowledge base](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/concepts/knowledge-base) for answering common enterprise FAQs (such as Benefits and HR Information) and customized Adaptive Cards.  

In many cases, you can leverage [Azure Active Directory (AAD)](https://azure.microsoft.com/en-us/services/active-directory/) for single sign-on (SSO), though this may be limited by the channel itself and your specific requirements. 

![Enterprise Notifications sample architecture]({{site.baseurl}}/assets/images/enterprisenotifications-architecture.png)

## Proactive Notifications
The Enterprise Assistant sample includes proactive notifications, enabling scenarios such as: 

- Send notifications to your users that the Enterprise Assistant would like to start a conversation, thus allowing the user to trigger when they are ready to have this discussion (e.g., a user receives a notification "your training is due", allowing them to initiate the conversation about what training is required) 

- Initiate a proactive dialog with your users through an open channel such as Microsoft Teams (e.g., "Benefits enrollment just opened; would you like to know more about benefits?") 

There are many scenarios where an enterprise-focused Assistant needs to push activities to employees. It is important to consider the range of channels you may offer to users and whether they provide a persistent conversation over time and the channel itself supports proactive message delivery. Microsoft Teams is an example of a persistent channel enabling conversations to occur over a longer period and across a range of devices. This contrasts with Web Chat which is only available for the life of the browser window. 

In addition to these common channels, mobile devices are another key end-user channel and these same notifications/messages should be delivered as appropriate to these devices. 

This sample demonstrates how to build a notification broadcast solution using a Virtual Assistant and related Azure resources. Each implementation will vary significantly, so this is available as a minimum viable product (MVP) to get started. 

## Supported scenarios

The majority of the skills connected to this sample are [experimental skills]({{site.baseurl}}/reference/skills/experimental), which means they are early prototypes of Skills and are likely to have rudimentary language models, limited language support and limited testing. These skills demonstrate a variety of skill concepts and provide great examples to get you started. This sample demonstrates the following scenarios:

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

#### [IT Service Management (ITSM) Skill]({{site.baseurl}}/skills/samples/experimenta/#it-service-management-skill)
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


#### [News Skill]({{site.baseurl}}/skills/samples/experimenta/#news-skill)
{:.no_toc}
##### Find news articles 
{:.no_toc}
- *What’s the latest news on technology?* 
- *What news is currently trending?* 

#### [Phone Skill]({{site.baseurl}}/skills/samples/experimenta/#phone-skill)
{:.no_toc}
##### Make an outgoing call
{:.no_toc}
- *Call Sanjay Narthwani* 
- *Call 867 5309* 
- *Make a call* 

#### [To Do Skill]({{site.baseurl}}/skills/samples/to-do)
{:.no_toc}
##### Add a task 
{:.no_toc}
- *Add some items to the shopping notes* 
- *Put milk on my grocery list* 
- *Create task to meet Leon after 5:00 PM* 

#### [Weather Skill]({{site.baseurl}}/skills/samples/experimental/#weather-skill)
{:.no_toc}
##### Get the forecast
{:.no_toc}
- *What’s the weather today?* 

## Deploy
Test

## Download transcripts
Test


## Notes
{:.no_toc}

THESE ARE NOTES FROM OLD ENTERPRISE NOTIFICATIONS DOC
### Event Producer
{:.no_toc}

Azure Functions are used to collect events from upstream systems and convert them into a canonical event schema before handing over to the Event Hub for centralized handling. In this sample, we simulate this event generation functionality for ease of testing by using the console application located [here](/samples/EnterpriseNotification/EventProducer).

### Azure Event Hub
{:.no_toc}

In this sample, the Azure Event Hub is the centralized service that manages events gathered from different parts of the system and sent through the Azure Function aforementioned. For any event to reach an end user, it has to flow into the Azure Event Hub first.

### Azure Functions - Event Handler
{:.no_toc}

After an event is posted to the Azure Event Hub, an Azure Function service is triggered to process them. The background to the use of Azure Functions is as follows:

- Azure Functions natively support triggers against a variety of Azure services, Event Hub trigger is one of these.
- Azure Functions scales against Event Hub services by managing locks on partitions of the Azure Event Hub internally as part of the framework.

#### Notification Handler (Trigger)
{:.no_toc}

The triggering element of the Azure function is handled as part of the [EventHandler](/samples/EnterpriseNotification/EventHandler). The **Run** method within [Function1.cs]({{site.repo}}/samples/EnterpriseNotification/EventHandler/Function1.cs) is automatically invoked once an event is available.

#### Notification Handler (Run)
{:.no_toc}

Following the trigger the following steps are performed as part of the same [EventHandler]({{site.repo}}/samples/EnterpriseNotification/EventHandler) example:

- Unpack the event
- Read from a UserPreference store to check user's profile settings
- If the user has 'SendNotificationToMobileDevice' flag to be true, then send a notification to user's mobile device with the event content.
- If the user has 'SendNotificationToConversation' flag to be true, then send a message to the bot with the event content.

This sample uses CosmosDB as the UserPreference store but can be modified to reflect an existing store you may already have in place. The code will check if there's a record for the particular user. If not, it will then add a record with default settings of both destinations set to true.

This sample doesn't include the implementation for sending a notification to mobile devices as this requires additional configuration. You can refer to [this documentation](https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-aspnet-backend-ios-apple-apns-notification) for more information.

The message the Event Handler sends to the bot is an event Activity, with the name **BroadcastEvent** and value is set to the data received rom the Event Hub.

### Notification Hub
{:.no_toc}

[Notification Hubs](https://azure.microsoft.com/en-us/services/notification-hubs) provide the capability to delivery notifications to end user devices. Please refer to [this documentation](https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-aspnet-backend-ios-apple-apns-notification) for additional steps to perform this integration as needed.
