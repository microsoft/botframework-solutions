---
layout: tutorial
category: Solution Accelerators
subcategory: Enable proactive notifications
title: Run your solution
order: 5
---

# Tutorial: {{page.subcategory}}
{:.no_toc}
## {{page.title}}
{:.no_toc}

Now events can be sent to a user through your Virtual Assistant in an active conversation.

### Start a new conversation with your Virtual Assistant

In order for the notification to be received, the sample event has to use the same **user id** as in an existing conversation.
![Bot Framework Emulator settings]({{site.baseurl}}/assets/images/proactive-notifications/emulator-settings.png)

1. Open the **Bot Framework Emulator**.

1. Navigate to **Settings** and provide the same **user id** you set in the **EventProducer**.

1. Run your Virtual Assistant project.

1. Start a new conversation with your Virtual Assistant to create a proactive state record for future user.

## Send a sample notification with the Event Producer

1. Run the **EventProducer** project to generate a sample notification message.

1. Congratulations, you've received a proactive notification through your Virtual Assistant!

![Demonstration of a notification received in an existing bot conversation]({{site.baseurl}}/assets/images/enterprisenotification-demo.png)