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

## Feedback Mechanism
The **VirtualAssistantSample** project within the Enterprise Assistant sample has feedback implemented using a temporary solution. It's recommended to review the provided [MainDialog.cs](https://aka.ms/bfEnterpriseAssistantMainDialog) in parallel to this document to gain a full understanding of this implementation.

This implementation allows for feedback to be collected when a child dialog of main dialog ends. The user's feedback is recorded and sent to app insights and results will be visible in your [PowerBI dashboard](https://aka.ms/bfFeedbackDocPowerBiHeader).

## Steps to implement feedback

1. If you are using an older VA template it may contain references to `FeedbackMiddleware`. Check your bot adapter class (default adapter being `defaultadapter.cs`) and remove any reference to FeedbackMiddleware in your bot adapter.
1. To start collecting user feedback, add the feedback directory found in the **VirtualAssistantSample** project within the Enterprise Assistant sample to your VA. This will have all the utility and helper functions you will need.
1. Add feedback options to startup.cs

    ```csharp
        services.AddSingleton(new FeedbackOptions());
    ```

    FeedbackOptions consists of the following. You can alter any of the properties when you declare your options in startup.cs

      | Property | Description | Type | Default value |
    | -------- | ----------- | ---- |------------- |
    | FeedbackEnabled | Value determines whether or not user should be prompted for feedback when a dialog ends and returns to main dialog | **bool** | *True* |
    | FeedbackActions | Feedback options shown to the user. | **CardAction List** | 👍 / 👎 |
    | DismissAction | Option to dismiss request for feedback, or request for comment. | **CardAction** | *Dismiss*
    | FeedbackReceivedMessage | Message to show after user has provided feedback. | **string** | *Thanks for your feedback!* |
    | CommentsEnabled | Flag indicating whether the bot should prompt for free-form comments after user has provided feedback. | **bool** | false |
    | CommentPrompt | Message to show after user provided feedback if CommentsEnabled is true. | **string** | *Please add any additional comments in the chat.*
    | CommentReceivedMessage | Message to show after user provides a free-form comment. | **string** | *Your comment has been received.* |
    | FeedbackPromptMessage | Message to show after user when prompting for feedback | **string** | *Was that helpful?* |

    Example of altering any of the identified properties in startup.cs
    ```csharp
                services.AddSingleton(new FeedbackOptions
                {
                    CommentsEnabled = false,
                    FeedbackActions =
                    {
                        new Choice()
                        {
                            Action = new CardAction(ActionTypes.MessageBack, title: "👆", text: "positive", displayText: "👆"),
                            Value = "positive",
                        },
                        new Choice()
                        {
                            Action = new CardAction(ActionTypes.MessageBack, title: "👇", text: "negative", displayText: "👇"),
                            Value = "negative",
                        },
                    }
                });
      ```
1. Copy the feedback waterfall steps from Maindialog.cs in the Enterprise Assistant solution (RequestFeedback, RequestFeedbackComment, ProcessFeedback) and add them to your VAs mainDialog.cs waterfall flow.
    - Add corresponding steps and prompts to the Maindialog constructor (use enterprise assistant for reference)
1. When you copy the waterfall steps over ensure you resolve any unknown object/function references with the implementations added in step 1.
    - There are separate implementations in the lib, **do not use those for this feedback solution, use the implementations you manually added in step 1**.
1. If you have followed these steps and used the Enterprise Assistant as a reference then you should now have all you need.
1. Run through any dialog in your VA, when the dialog ends and returns to MainDialog.cs then the feedback flow should be triggered.
    - If the user ignores the feedback prompt and sends an unrelated query to the bot then they're query will skip feedback and be routed accordingly.
    - If the user provides any feedback at all then it will be logged with app insights and that telemetry will be part of your PowerBI dashboard

## View your feedback in Power BI
You can view your **Feedback** in the Feedback tab of the Conversational AI Dashboard.

![]({{site.baseurl}}/assets/images/analytics/virtual-assistant-analytics-powerbi-13.png)

[Learn how to set up your own Power BI dashboard.]({{site.baseurl}}/solution-accelerators/tutorials/view-analytics/1-intro/)
