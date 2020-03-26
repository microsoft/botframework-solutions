---
category: Virtual Assistant
subcategory: Handbook
title: Feedback
description: Collect feedback from users
order: 9
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

## Current State of Feedback
The feedback middleware approach has been deprecated since the .8 release of Microsoft.Bot.Solutions. 
With the 1.0 release we have implemented a temporary feedback mechanism which is outlined below. We will have an incremental release in the near future with a more robust feedback implementation that will be a part of the Microsoft.Bot.Solutions lib. As of now the Microsoft.Bot.Solutions lib still contains the middleware solution but it is marked as deprecated and it will not work as desired due to the waterfall flow of the VA since the .8 release. 

## Temporary Feedback Mechanism
The virtualAssistantSample project within the Enterprise Assistant sample has feedback implemented using our temporary solution. It will be useful to go through [MainDialog.cs within the EnterpriseVAs virtualAssistantSample](https://aka.ms/bfEnterpriseAssistantMainDialog) in parallel to this document to gain an understanding of the feedback implementation so you can then implement it in your VA.

This implementation allows for feedback to be collected when a child dialog of main dialog ends. The users feedback is recorded and sent to app insights and results will be visible in your PowerBI dashboard (discussed later in this doc).

## Steps to implement feedback

1) To start collecting user feedback, add the feedback directory found in the virtualAssistantSample project within the Enterprise Assistant sample to your VA. This will have all the utility and helper functions you will need.
1) Add feedback options to startup.cs
    ```csharp
            services.AddSingleton(new FeedbackOptions());
   ```

   FeedbackOptions consists of the following. You can alter any of the properties when you declare your options in startup.cs

  | Property | Description | Type | Default value |
| -------- | ----------- | ---- |------------- |
| FeedbackActions | Feedback options shown to the user. | **CardAction List** | 👍 / 👎 |
| DismissAction | Option to dismiss request for feedback, or request for comment. | **CardAction** | *Dismiss*
| FeedbackReceivedMessage | Message to show after user has provided feedback. | **string** | *Thanks for your feedback!* |
| CommentsEnabled | Flag indicating whether the bot should prompt for free-form comments after user has provided feedback. | **bool** | false |
| CommentPrompt | Message to show after user provided feedback if CommentsEnabled is true. | **string** | *Please add any additional comments in the chat.*
| CommentReceivedMessage | Message to show after user provides a free-form comment. | **string** | *Your comment has been received.* |
| FeedbackPromptMessage | Message to show after user when prompting for feedback | **string** | *Was that helpful?* |


3. Copy the feedback waterfall steps from Maindialog.cs in the Enterprise Assistant solution (RequestFeedback, RequestFeedbackComment, ProcessFeedback) and add them to your VAs mainDialog.cs waterfall flow.
    - Add corresponding steps and prompts to the Maindialog constructor (use enterprise assistant for reference)
    - Add the ```private bool _feedbackEnabled``` attribute to your MainDialog class and ensure it is set to true
4. When you copy the waterfall steps over ensure you resolve any unknown object/function references with the implementations added in step 1. 
    - There are separate implementations in the lib, do not use those for this feedback solution, use the implementations you manually added in step 1.
4. If you have followed these steps and used the Enterprise Assistant as a reference then you should now have all you need.
5. Run through any dialog in your VA, when the dialog ends and returns to MainDialog.cs then the feedback flow should be triggered. 
    - If the user ignores the feedback prompt and sends an unrelated query to the bot then they're query will skip feedback and be routed accordingly. 
    - If the user provides any feedback at all then it will be logged with app insights and that telemetry will be part of your PowerBI dashboard

## View your feedback in Power BI
You can view your **Feedback** in the Feedback tab of the Conversational AI Dashboard. 

![]({{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-13.png)

[Learn how to set up your own Power BI dashboard.]({{site.baseurl}}/solution-accelerators/tutorials/view-analytics/1-intro/)