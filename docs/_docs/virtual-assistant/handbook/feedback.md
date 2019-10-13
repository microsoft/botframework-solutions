---
category: Virtual Assistant
subcategory: Handbook
title: Feedback
description: Collect feedback from users
order: 9
---

# {{ page.title }}
{:.no_toc}

## In this topic
{:.no_toc}

* 
{:toc}

## Add and configure the middleware
To start collecting user feedback, add the following code block in your adapter class (DefaultAdapter.cs in the Virtual Assistant and Skill templates):

```csharp
Use(new FeedbackMiddleware(conversationState, telemetryClient, new FeedbackOptions()));
```

This enables the FeedbackMiddleware with the following default settings:

| Property | Description | Type | Default value |
| -------- | ----------- | ---- |------------- |
| FeedbackActions | Feedback options shown to the user. | `List<CardAction>` | 👍 / 👎 |
| DismissAction | Option to dismiss request for feedback, or request for comment. | `CardAction` | *Dismiss*
| FeedbackReceivedMessage | Message to show after user has provided feedback. | `string` | *Thanks for your feedback!* |
| CommentsEnabled | Flag indicating whether the bot should prompt for free-form comments after user has provided feedback. | `bool` | false |
| CommentPrompt | Message to show after user provided feedback if CommentsEnabled is true. | `string` | *Please add any additional comments in the chat.*
| CommentReceivedMessage | Message to show after user provides a free-form comment. | `string` | *Your comment has been received.* |

Here is an example customization with different feedback options and comments enabled:

```csharp
Use(new FeedbackMiddleware(conversationState, telemetryClient, new FeedbackOptions()
{
      FeedbackActions = new List<CardAction>()
      {
        new CardAction(ActionTypes.PostBack, title: "🙂", value: "positive"),
        new CardAction(ActionTypes.PostBack, title: "😐", value: "neutral"),
        new CardAction(ActionTypes.PostBack, title: "🙁", value: "negative"),
      };
      CommentsEnabled = true
}));
```

## Request feedback
You can request feedback from your users using the following code snippet:

```csharp
FeedbackMiddleware.RequestFeedbackAsync(turnContext, "your-tag")
```
> Replace "your-tag" with a custom label for your feedback to be shown in Power BI dashboard. For example, QnA Maker feedback might be labelled "qna".

## Request feedback in skills
To enable requesting feedback in your skills, you must either be using the same state storage and Application Insights services as your Virtual Assistant (with FeedbackMiddleware enabled) or you need to follow the above steps to configure the FeedbackMiddleware in your adapter.

After the middleware is configured, you can request feedback as usual.

## View your feedback in Power BI
You can view your **Feedback** in the Feedback tab of the Conversational AI Dashboard. 

More information on Power BI and Analytics in Virtual Assistant can be found [here]({{site.repo}}/reference/analytics/powerbi/).
