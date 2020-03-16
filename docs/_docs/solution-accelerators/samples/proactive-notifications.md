---
category: Solution Accelerators
subcategory: Samples
title: Proactive notifications
order: 2
toc: true
---
# {{ page.title }}
{:.no_toc}

There are many scenarios where a Virtual Assistant needs to push activities to users. It is important to consider the range of channels you may offer to users and whether they provide a persistent conversation over time and the channel itself supports proactive message delivery. Microsoft Teams is an example of a persistent channel enabling conversations to occur over a longer period and across a range of devices. This contrasts with Web Chat which is only available for the life of the browser window. 

In addition to these common channels, mobile devices are another key end-user channel and these same notifications/messages should be delivered as appropriate to these devices. 

This sample demonstrates how to build a notification broadcast solution using a Virtual Assistant and related Azure resources. Each implementation will vary significantly, so this is available as a minimum viable product (MVP) to get started. 

This sample includes proactive notifications, enabling scenarios such as: 

- Send notifications to your users that the Virtual Assistant would like to start a conversation, thus allowing the user to trigger when they are ready to have this discussion (e.g., a user receives a notification "your training is due", allowing them to initiate the conversation about what training is required) 

- Initiate a proactive dialog with your users through an open channel such as Microsoft Teams (e.g., "Benefits enrollment just opened; would you like to know more about benefits?") 

![Proactive notifications sample architecture]({{site.baseurl}}/assets/images/ProactiveNotificationsDrawing.PNG)

## Deploy

An automated deployment will be available in the [Enterprise Assistant sample]({{site.baseurl}}/solution-accelerators/assistants/enterprise-assistant), otherwise you can follow the tutorial in **Next steps** to manually provision the necessary Azure resources.

## Next Steps

<div class="card-deck">
    {% include button.html params=site.data.button.proactive %}
</div>