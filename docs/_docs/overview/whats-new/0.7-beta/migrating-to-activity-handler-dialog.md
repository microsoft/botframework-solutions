---
category: Overview
subcategory: What's New
language: 0_7_release
title: Migrating to ActivityHandlerDialog
description: Step-by-step instructions for migrating a dialog from RouterDialog to ActivityHandlerDialog
date: 2019-11-26
order: 2
toc: true
---

# Beta Release 0.7
## {{ page.title }}
{:.no_toc}
{{ page.description }}

### Instructions

1. Change MainDialog to derive from `ActivityHandlerDialog`
1. Rename `RouteAsync` method to `OnMessageActivityAsync` 
    - Remove this line:
        ```
        var turnResult = EndOfTurn;
        ```
    - Remove this code block:
        ```
        if (turnResult != EndOfTurn)
        {
            await CompleteAsync(dc);
        }
        ```
1. Rename `OnEventAsync` to `OnEventActivityAsync`
1. In `OnCancel`
    - Remove the following:
        ```
        await CompleteAsync(dc);
        await dc.CancelAllDialogsAsync();
        ```
    - Change interruption action to `InterruptionAction.End`
1. In `OnHelp`
    - Change interruption action to `InterruptionAction.Resume`
1. In `OnLogout`
    - Change interruption action to `InterruptionAction.End`
1. Replace `OnStartAsync` with the following:

    ```
    // Runs when the dialog stack is empty, and a new member is added to the conversation. Can be used to send an introduction activity.
    protected override async Task OnMembersAddedAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
    {
        await innerDc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.WelcomeMessage));
    }
    ```
1. Rename CompleteAsync to OnDialogComleteAsync and change DialogTurnResult to object.