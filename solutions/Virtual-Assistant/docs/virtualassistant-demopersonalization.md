# Personalizing a Virtual Assistant Demo

## Overview

While the Virtual Assistant Solution provides out-of-the-box functionality to demonstrate an end-to-end experience, it is often wise to customize key components that can tailor to your audience.
To get started, learn how to clone the repository & deploy your Azure resources by reading [Virtual Assistant Deployment](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/docs/virtualassistant-createvirtualassistant.md).

## Project Structure

The folder structure of your Virtual Assistant is shown below.

    | - Assistant                           // Directory for the core Virtual Assistant
        | - YOURBOT.bot            // The .bot file containing all of your Bot configuration including dependencies
        | - README.md                       // README file containing links to documentation
        | - Program.cs                      // Default Program.cs file
        | - Startup.cs                      // Core Bot Initialisation including Bot Configuration LUIS, Dispatcher, etc. 
        | - appsettings.json                // References above .bot file for Configuration information. App Insights key
        | - CognitiveModels     
            | - LUIS                        // .LU files containing base conversational intents (Greeting, Help, Cancel)
            | - QnA                         // .LU files containing example QnA items
        | - DeploymentScripts               // msbot clone recipe for deployment
        | - Dialogs                         // Main dialogs sit under this directory
            | - Main                        // Root Dialog for all messages
                | - MainDialog.cs           // Dialog Logic
                | - MainResponses.cs        // Dialog responses
                | - Resources               // Adaptive Card JSON, Resource File
            | - Onboarding
                | - OnboardingDialog.cs     // Onboarding dialog Logic
                | - OnboardingResponses.cs  // Onboarding dialog responses
                | - OnboardingState.cs      // Localised dialog state
                | - Resources               // Resource File
            | - Cancel
            | - Escalate
            | - Signin
        | - Middleware                      // Telemetry, Content Moderator
        | - ServiceClients                  // SDK libraries, example GraphClient provided for Auth example
    | - Skills                              // Directory for the Virtual Assistant skills
        | - CalendarSkill
        | - EmailSkill
        | - PointOfInterestSkill
        | - Tests
        | - ToDoSkill

## Update the Introduction

When a new conversation is started with a Virtual Assistant, it receives a `ConversationUpdate` Activity and begins the `MainDialog`. 
The first Activity the Virtual Assistant will send displays an introduction card, which is found under [/assistant/Dialogs/Main/Resources](https://github.com/Microsoft/AI/tree/master/solutions/Virtual-Assistant/src/csharp/assistant/Dialogs/Main/Resources). 
The introduction is presented with an [Adaptive Card](https://adaptivecards.io/), where UX elements can be defined once and rendered appropriate to your client. You can copy the JSON below and paste in in [Adaptive Cards Designer](https://adaptivecards.io/designer/) to experiment with yourself.

![Virtual Assistant Introduction Card](media/virtualAssistant-introductionCard.png)

```
{
  "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
  "type": "AdaptiveCard",
  "version": "1.0",
  "speak": "Welcome to your Virtual Assistant! Now that you're up and running, let's get started.",
  "body": [
    {
      "type": "Image",
      "url": "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQtB3AwMUeNoq4gUBGe6Ocj8kyh3bXa9ZbV7u1fVKQoyKFHdkqU",
      "size": "stretch"
    },
    {
      "type": "TextBlock",
      "spacing": "medium",
      "size": "default",
      "weight": "bolder",
      "text": "Welcome to **your** Virtual Assistant!",
      "speak": "Welcome to your Virtual Assistant",
      "wrap": true,
      "maxLines": 0
    },
    {
      "type": "TextBlock",
      "size": "default",
      "isSubtle": "yes",
      "text": "Now that you have successfully run your Virtual Assistant, follow the links in this Adaptive Card to explore further.",
      "speak": "Now that your up and running let's get started.",
      "wrap": true,
      "maxLines": 0
    }
  ],
  "actions": [
    {
      "type": "Action.OpenUrl",
      "title": "Documentation",
      "url": "https://aka.ms/customassistantdocs"
    },
    {
     "type": "Action.OpenUrl",
      "title": "Linked Accounts",
      "url": "https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/docs/customassistant-linkedaccounts.md"
    },
    {
      "type": "Action.OpenUrl",
      "title": "Skills",
      "url": "https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/docs/customassistant-skills.md"
    }
  ]
}
```

## Update the FAQ with QnAMaker

The FAQ provided features commonly asked questions about the Bot Framework, but you may wish to provide industry-specific samples.

To update an existing QnAMaker Knowledge Base, perform the following steps:
1. Make changes to your QnAMaker Knowledge Base via the [LuDown](https://github.com/Microsoft/botbuilder-tools/tree/master/packages/Ludown) and [QnAMaker](https://github.com/Microsoft/botbuilder-tools/tree/master/packages/QnAMaker) CLI tools or the [QnAMaker Portal](https://qnamaker.ai).
2. Run the following command to update your Dispatch model to reflect your changes (ensures proper message routing):
```shell
    dispatch refresh --bot "YOURBOT.bot" --secret YOURSECRET
```

## Sample Transcript

For ideas on the demo script you'd like to present, see our attached sample (demonstrating all skills & authentication) at.
This can be opened in the [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator/wiki).
