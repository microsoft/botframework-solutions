---
category: Overview
subcategory: What's New
language: 0_8_release
title: MainDialog updates
date: 2020-02-03
description: Steps for updating to Waterfall MainDialog implementation
order: 2
toc: true
---

# Beta Release 0.8
## {{ page.title }}
{:.no_toc}
{{ page.description }}

### Steps
1. Move introduction logic to DefaultActivityHandler OnMembersAddedAsync method.
1. Copy routing logic from OnMessageActivityAsync method into RouteStepAsync method.
    - Change any switch statement cases that start new dialogs to return the result of BeginDialogAsync like below for proper dialog flow management:

        **Previous implementation**
        ```csharp
        await dc.BeginDialogAsync("Faq");
        ```

        **New Implementation**
        ```csharp
        return await stepContext.BeginDialogAsync("Faq");
        ```
1. Copy interruption logic from OnInterruptDialogAsync into InterruptDialogAsync

    - InterruptionActions have been deprecated, so each interruption should now manage the dialog continuation/cancellation on its own. 

        **Previous implementation**
        ```csharp
        case GeneralLuis.Intent.Cancel:
            {
                await dc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("CancelledMessage"));
                await dc.CancelAllDialogsAsync();
                return InterruptionAction.End;
            }
        ```

        **New Implementation**
        ```csharp
        case GeneralLuis.Intent.Cancel:
        {
            await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("CancelledMessage", userProfile));
            await innerDc.CancelAllDialogsAsync();
            await innerDc.BeginDialogAsync(InitialDialogId);
            interrupted = true;
            break;
        }
        ```
