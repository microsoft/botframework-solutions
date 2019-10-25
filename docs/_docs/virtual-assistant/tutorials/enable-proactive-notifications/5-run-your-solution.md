---
layout: tutorial
category: Virtual Assistant
subcategory: Enable proactive notifications
title: Run your solution
order: 5
---

# Tutorial: {{page.subcategory}}

## {{page.title}}

Now events can be sent to a user through your Virtual Assistant in an active conversation.

### Bot Framework Emulator
{:.no_toc}

Event generation must generate Events with the same `UserId` as the Emulator is using so the existing conversation can be matched and notifications can be delivered.

![UserId Settings]({{ site.baseurl }}/assets/images/emulator-userid.png)

1. In the **Bot Framework Emulator**, navigate to **Settings** and provide a guid to represent a simulated user ID. This will ensure any conversations with your Assistant use the same user ID.

1. Begin a conversation with your Assistant to create a proactive state record for future user.

## Event Producer

1. Copy the user ID used in the **Bot Framework Emulator** into the `SendMessagesToEventHub` method within `Program.cs` of the **Event Producer**. 
This ensures any notifications sent are routed to your active conversation.


1. Run the **Event Producer** to generate a message and observe that the message is shown within your session.

![Enterprise Notification Demo]({{ site.baseurl }}/assets/images/enterprisenotification-demo.png)