---
layout: tutorial
category: Virtual Assistant
subcategory: Enable proactive notifications
title: Run your solution
order: 5
toc: true
---

# Tutorial: {{page.subcategory}}
{:.no_toc}
## {{page.title}}
{:.no_toc}

Now events can be sent to a user through your Virtual Assistant in an active conversation.

### Start a new conversation with your Virtual Assistant

The sample notification event has to match the same **user id** as in an existing conversation.

![UserId Settings]({{ site.baseurl }}/assets/images/emulator-userid.png)

1. Open the **Bot Framework Emulator** 

1. In the **Bot Framework Emulator**, navigate to **Settings** and provide a guid to represent a simulated user ID. This will ensure any conversations with your Assistant use the same user ID.

1. Begin a conversation with your Assistant to create a proactive state record for future user.

## Send a sample notification with the Event Producer

1. Run the **Event Producer** project to generate a sample notification message. The message will appear in the existing 
and observe that the message is shown within your session.

![Enterprise Notification Demo]({{ site.baseurl }}/assets/images/enterprisenotification-demo.png)