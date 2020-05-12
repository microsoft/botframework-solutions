---
category: Overview
subcategory: What's New
language: 1_0_release
date: 2020-05-11
title: Migrate existing Virtual Assistant to not use Chitchat in Dispatch
description: Explains the steps to migrate an existing VA to remove `Chitchat` from Dispatch
order: 4
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

# Remove Chitchat intent from Dispatch

As part of the default deployment, the Virtual Assistant template creates two LUIS apps: one called `<NameofBot><LangLocale>_Dispatch` and the other `<NameOfBot><LangLocale>_General`, along with 2 QnA Knowledge Bases (KBs) for replying to `Chitchat` and `Faq` requests. The `Chitchat` KB in particular can be large and is used to provide a [personality](https://github.com/Microsoft/BotBuilder-PersonalityChat) to the bot. 

The `Dispatch` LUIS App is used to determine if a user utterance has a `General`, `Skill` or `Chitchat` intent. Once the  intent is recognized, the user utterance is routed to the specific `LUIS` or `QnA Kb` to fetch a more specific response for the user.

This architecture works well if there are relatively similar numbers of training samples per intent (i.e. if the dataset is balanced).  However, our `Chitchat` personality dataset tends to be very large relative to the other datasets. Once a dataset becomes imbalanced relative to the others, LUIS has a tendency to overfit the user utterance to that intent [^fn1].

To combat the issue of overfitting on `Chitchat`, we have removed the `Chitchat` intent from the `Dispatch` Luis App and only route to the Chitchat KB in the event that there are no other matches. In v1.0, this is the default behavior.

This guide shows you how to remove the `Chitchat` intent from the `Dispatch` app if you are migrating from an earlier version of the Virtual Assistant Template.  In v1.0, this is the default behavior and nothing needs to be done.

## Changes

### C#

1. In the `Dialogs` folder of your Virtual Assistant Project, add this new function to `MainDialog.cs`:
    ```csharp
        /// <summary>
        /// A simple set of heuristics to govern if we should invoke the personality <see cref="QnAMakerDialog"/>.
        /// </summary>
        /// <param name="stepContext">Current dialog context.</param>
        /// <param name="dispatchIntent">Intent that Dispatch thinks should be invoked.</param>
        /// <param name="dispatchScore">Confidence score for intent.</param>
        /// <param name="threshold">User provided threshold between 0.0 and 1.0, if above this threshold do NOT show chitchat.</param>
        /// <returns>A <see cref="bool"/> indicating if we should invoke the personality dialog.</returns>
        private bool ShouldBeginChitChatDialog(WaterfallStepContext stepContext, DispatchLuis.Intent dispatchIntent, double dispatchScore, double threshold = 0.5)
        {
            if (threshold < 0.0 || threshold > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(threshold));
            }

            if (dispatchIntent == DispatchLuis.Intent.None)
            {
                return true;
            }

            if (dispatchIntent == DispatchLuis.Intent.l_General)
            {
                // If dispatch classifies user query as general, we should check against the cached general Luis score instead.
                var generalResult = stepContext.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralResult);
                if (generalResult != null)
                {
                    (var _, var generalScore) = generalResult.TopIntent();
                    return generalScore < threshold;
                }
            }
            else if (dispatchScore < threshold)
            {
                return true;
            }

            return false;
        }
    ```
1. In the `RouteStepAsync` method in `MainDialog.cs`, look for the line that says:
    ```csharp
    if (dispatchIntent == DispatchLuis.Intent.q_Chitchat)
    ```
    and replace this with the newly added method:
    ```csharp
    if (ShouldBeginChitChatDialog(stepContext, dispatchIntent, dispatchScore))
    ```
1. `deploy-cognitive-models.ps1` and `update-cognitive-models.ps1` must be updated to prevent the ChitChat kb from being added to Dispatch. Look for sections that call the `dispatch add` tool with `--type "qna"`: 
    ```powershell
        if ($dispatch) {
            Write-Host "> Adding $($langCode) $($kb.id) kb to dispatch file ..." -NoNewline
            dispatch add `
                --type "qna" `
                --name $kb.name `
                --id $kb.kbId  `
                --key $kb.subscriptionKey `
                --intentName "q_$($kb.id)" `
                --dispatch $dispatchFile `
                --dataFolder $(Join-Path $dispatchFolder $langCode) 2>> $logFile | Out-Null
            Write-Host "Done." -ForegroundColor Green
        }
    ```
    and change the conditions to prevent the Chitchat data from being added to dispatch e.g.:
    ```powershell
    if ($dispatch -and -not @("Chitchat").Contains($kb.id)) {
        ...
    }
    ```
1. Run `update-cognitive-models.ps1`.  Log in to [luis.ai](https://luis.ai), and inspect your `Dispatch` app.  The `Chitchat` intent should not be present.  The file `services/DispatchLuis.cs` should also have been automatically updated when `update-cognitive-models.ps1` ran `bf luis:generate:cs` as one of the script steps.
    ```powershell
        if ($useLuisGen) {
            # Update dispatch.cs file
            Write-Host "> Running LuisGen for Dispatch app..." -NoNewline
			bf luis:generate:cs `
                --in $(Join-Path $dispatchFolder $langCode "$($dispatch.name).json") `
                --className "DispatchLuis" `
                --out $lgOutFolder `
                --force 2>> $logFile | Out-Null 
            Write-Host "Done." -ForegroundColor Green
		}
    ```
    This updates the `services/DispatchLuis.cs` `Intent` enum to not include `Chitchat` anymore. 

1.  Please see Pull Request [#3291](https://github.com/microsoft/botframework-solutions/pull/3291) for more details


### Typescript

1. Please see Pull Request [3304](https://github.com/microsoft/botframework-solutions/pull/3304) for more details


[^fn1]: [Training a Chatbot with Microsoft LUIS: Effect of Imbalance on Prediction Accuracy](https://dl.acm.org/doi/pdf/10.1145/3379336.3381494) E.Ruane, R.Young & A.Ventrisque. Mar 20, 2020