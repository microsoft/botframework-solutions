# Google Adapter for Bot Builder v4 .NET SDK - ***PREVIEW***

## Build status
| Branch | Status | Recommended NuGet package version |
| ------ | ------ | ------ |
| master | [![Build status](https://ci.appveyor.com/api/projects/status/b9123gl3kih8x9cb?svg=true)](https://ci.appveyor.com/project/garypretty/botbuilder-community) | Preview [available via MyGet (version 4.6.4-beta0036)](https://www.myget.org/feed/botbuilder-community-dotnet/package/nuget/Bot.Builder.Community.Adapters.Google/4.6.4-beta0036) |

## Description

This is part of the [Bot Builder Community Extensions](https://github.com/botbuildercommunity) project which contains various pieces of middleware, recognizers and other components for use with the Bot Builder .NET SDK v4.

The Google Adapter allows you to add an additional endpoint to your bot for custom Google Actions. The Google endpoint can be used
in conjunction with other channels meaning, for example, you can have a bot exposed on out of the box channels such as Facebook and 
Teams, but also via a Google Action (as well as side by side with the Alexa / Twitter Adapters also available from the Bot Builder Community Project).

Incoming Google Action requests are transformed, by the adapter, into Bot Builder Activties and then when your bot responds, the adapter transforms the outgoing Activity into a Google response.

The adapter currently supports the following scenarios;

* Support for voice based Google actions
* Support for Basic Card, Table Card and Signin Card
* Send Lists and Carousels to allow the user to select from a visual list
* Automatic conversion of Suggested Actions on outgoing activity into Google Suggestion Chips
* Full incoming request from Google is added to the incoming activity as ChannelData

## Installation

The preview of the next version of the Google Adapter is [available via MyGet (version 4.6.4-beta0036)](https://www.myget.org/feed/botbuilder-community-dotnet/package/nuget/Bot.Builder.Community.Adapters.Google/4.6.4-beta0036).

To install into your project use the following command in the package manager.  If you wish to use the Visual Studio package manager, then add https://www.myget.org/F/botbuilder-community-dotnet/api/v3/index.json as an additional package source.
```
    PM> Install-Package Bot.Builder.Community.Adapters.Google -Version 4.6.4-beta0036 -Source https://www.myget.org/F/botbuilder-community-dotnet/api/v3/index.json
```

## Sample

Sample bot, showing examples of Google specific functionality using the current preview is available [here](https://github.com/BotBuilderCommunity/botbuilder-community-dotnet/tree/feature/google-adapter-refactor/samples/Google%20Adapter%20Sample).

## Usage

* [Prerequisites](#prerequisites)
* [Create an Actions on Google project](#create-an-actions-on-google-project)
* [Wiring up the Google adapter in your bot](#wiring-up-the-google-adapter-in-your-bot)
* [Complete configuration of your Action package](#complete-configuration-of-your-action-package)
* [Test your Google Action](#test-your-google-action) - Test your bot in the Google Actions simulator
* [Customising your conversation](#customising-your-conversation) - Learn about controlling end of session and use of basic card, table card, list and carousel

In this article you will learn how to connect a bot to Google Assistant using the Google adapter.  This article will walk you through modifying the EchoBot sample to connect it to a skill.

### Prerequisites

* The [EchoBot sample code](https://github.com/microsoft/BotBuilder-Samples/tree/master/samples/csharp_dotnetcore/02.echo-bot)

* Access to the Actions on Google developer console with sufficient permissions to login to create / manage projects at  [https://console.actions.google.com/](https://console.actions.google.com/). If you do not have this you can create an account for free.

### Create an Actions on Google project

1. Log into the [Actions on Google console](https://console.actions.google.com/) and then click the **New project** button.

2. In the **New Project** popup dialog, enter a name for your project and choose your desired language and country or region, then click **Create project**.

![New proejct details](/libraries/Bot.Builder.Community.Adapters.Google/media/project-name.PNG?raw=true)

3. You will now be asked to choose the development experience for your project.  In the **More Options** area below, choose **Actions SDK**.

![Project development experience](/libraries/Bot.Builder.Community.Adapters.Google/media/project-development-experience.PNG?raw=true)

4. A popup window will now be shown advising you how to **Use Actions SDK to add Actions to your project**.  The following steps will walk you through this process. Make note of the **gactions** command shown, as you'll need to use this command when uploading your Action package in a later step.

![Project development experience](/libraries/Bot.Builder.Community.Adapters.Google/media/actions-sdk-getting-started.PNG?raw=true)

5. Click the **OK** button, which will take you to your new project's **Overview** page.

6. Click the **Develop** tab at the top and you will be able to enter a **Display Name** for your new action. This **Display Name** will also be your action's invocation name, which people will use to talk to your action on Google Assistant.  For example, if the display name was 'Adapter Helper', then people would say 'Hey Google, talk to Adapter Helper'.  Enter your desired display / invocation name and click the **Save** button.

![Simulator](/libraries/Bot.Builder.Community.Adapters.Google/media/display-name.PNG?raw=true)

7. Download the **gactions CLI** tool for your platform from [https://developers.google.com/assistant/tools/gactions-cli](https://developers.google.com/assistant/tools/gactions-cli) and save it in a location of your choice.

8. You now need to create an Action package. Open a text editor and create a file with the following content. 

```json
{
  "actions": [
    {
      "description": "Default Intent",
      "name": "MAIN",
      "fulfillment": {
        "conversationName": "bot-application"
      },
      "intent": {
        "name": "actions.intent.MAIN",
        "trigger": {
          "queryPatterns": [
            "talk to YOUR-ACTION-DISPLAY-NAME"
          ]
        }
      }
    }
  ],
  "conversations": {
    "bot-application": {
      "name": "bot-application",
      "url": ""
    }
  },
  "locale": "en"
}
```

9. You need to replace ***YOUR-ACTION-DISPLAY-NAME***, within the **trigger** section of the document, with the display name that you chose in the previous step. For example, if your display name was 'Adapter Helper', then your updated trigger would look like this.

```json
        "trigger": {
          "queryPatterns": [
            "talk to adapter helper"
          ]
        }
```

10. Save your new Action package file.  We will update your Actions SDK project later, using the **gactions** cli tool you previously downloaded, but you still need to update the URL to your bot's endpoint. To obtain the correct endpoint, you need to wire up the Google Adapter into your bot and deploy it.

### Wiring up the Google adapter in your bot

Before you can complete the configuration of your Actions on Google project, you need to wire up the Google adapter into your bot.

#### Install the Google adapter NuGet package

Add  the [Bot.Builder.Community.Adapters.Google](https://www.nuget.org/packages/Bot.Builder.Community.Adapters.Google/) NuGet package. For more information on using NuGet, see [Install and manage packages in Visual Studio](https://aka.ms/install-manage-packages-vs)

#### Create a Google adapter class

Create a new class that inherits from the ***GoogleAdapter*** class. This class will act as our adapter for Google Assistant. It includes error handling capabilities (much like the ***BotFrameworkAdapterWithErrorHandler*** class already in the sample, used for handling requests from Azure Bot Service).  

```csharp
public GoogleAdapterWithErrorHandler(ILogger<GoogleAdapter> logger, GoogleAdapterOptions adapterOptions)
    : base(adapterOptions, logger)
{
    OnTurnError = async (turnContext, exception) =>
    {
        // Log any leaked exception from the application.
        logger.LogError($"Exception caught : {exception.Message}");

        // Send a catch-all apology to the user.
        await turnContext.SendActivityAsync("Sorry, it looks like something went wrong.");
    };
}
```

You will also need to add the following using statements.

```cs
using Bot.Builder.Community.Adapters.Google;
using Microsoft.Extensions.Logging;
```

#### Create a new controller for handling Google Assistant requests

You now need to create a new controller which will handle requests from your Google action, on a new endpoing 'api/google' instead of the default 'api/messages' used for requests from Azure Bot Service Channels.  By adding an additional endpoint to your bot, you can accept requests from Bot Service channels (or additional adapters), as well as from Google, using the same bot.

```csharp
[Route("api/google")]
[ApiController]
public class GoogleController : ControllerBase
{
    private readonly GoogleAdapter Adapter;
    private readonly IBot Bot;

    public GoogleController(GoogleAdapter adapter, IBot bot)
    { 
        Adapter = adapter;
        Bot = bot;
    }

    [HttpPost]
    public async Task PostAsync()
    {
        // Delegate the processing of the HTTP POST to the adapter.
        // The adapter will invoke the bot.
        await Adapter.ProcessAsync(Request, Response, Bot);
    }
}
```

You will also need to add the following using statements.

```cs
using System.Threading.Tasks;
using Bot.Builder.Community.Adapters.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
```

#### Inject Google Adapter and Google Adapter Options In Your Bot Startup.cs

1. Add the following code into the ***ConfigureServices*** method within your Startup.cs file, which will register your Google adapter and make it available for your new controller class. We will also create and register a ***AlexaAdapterOptions*** class, which will contain necessary information for your adapter to function correctly.  You need to replace "YOUR-ACTION-DISPLAY-NAME" with the display name you gave to your action, which you also specified in your action package in the earlier step. You also need to replace "YOUR-PROJECT-ID" with the ID of your **Actions on Google** project - you can find this at the end of the **gactions** cli command you made a note of in an earlier step.

```csharp
    // Create the Google Adapter
    services.AddSingleton<GoogleAdapter, GoogleAdapterWithErrorHandler>();

    // Create GoogleAdapterOptions
    services.AddSingleton(sp =>
    {
        return new GoogleAdapterOptions()
        {
            ActionInvocationName = "YOUR-ACTION-DISPLAY-NAME",
            ActionProjectId = "YOUR-PROJECT-ID"
        };
    });
```

2. Once added, your ***ConfigureServices*** method shold look like this.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

    // Create the default Bot Framework Adapter (used for Azure Bot Service channels and emulator).
    services.AddSingleton<IBotFrameworkHttpAdapter, BotFrameworkAdapterWithErrorHandler>();

    // Create the Google Adapter
    services.AddSingleton<GoogleAdapter, GoogleAdapterWithErrorHandler>();

    // Create GoogleAdapterOptions
    services.AddSingleton(sp =>
    {
        return new GoogleAdapterOptions()
        {
            ActionInvocationName = "YOUR-ACTION-DISPLAY-NAME",
            ActionProjectId = "YOUR-PROJECT-ID"
        };
    });

    // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
    services.AddTransient<IBot, EchoBot>();
}
```

3. You will also need to add the following using statement, in addition to those already present in the startup.cs file.

```cs
using Bot.Builder.Community.Adapters.Google;
```

### Complete configuration of your Action package

Now that you have wired up the adapter in your bot project, the final steps are to configure the endpoint, in your action package, to which requests will be posted to when your action is invoked, pointing it to the correct endpoint on your bot.

1. To complete this step, [deploy your bot to Azure](https://aka.ms/bot-builder-deploy-az-cli) and make a note of the URL to your deployed bot. Your Google messaging endpoint is the URL for your bot, which will be the URL of your deployed application (or ngrok endpoint), plus '/api/alexa' (for example, `https://yourbotapp.azurewebsites.net/api/Google`).

> [!NOTE]
> If you are not ready to deploy your bot to Azure, or wish to debug your bot when using the Alexa adapter, you can use a tool such as [ngrok](https://www.ngrok.com) (which you will likely already have installed if you have used the Bot Framework emulator previously) to tunnel through to your bot running locally and provide you with a publicly accessible URL for this. 
> 
> If you wish create an ngrok tunnel and obtain a URL to your bot, use the following command in a terminal window (this assumes your local bot is running on port 3978, alter the port numbers in the command if your bot is not).
> 
> ```
> ngrok.exe http 3978 -host-header="localhost:3978"
> ```

2. Go back to the action package you created in the text editor and replace the URL for the conversation endpoint with your bot's Google endpoint, such as https://yourbotapp.azurewebsites.net/api/google.  For example, the conversation section within your action package may look like this.

```json
  "conversations": {
    "bot-application": {
      "name": "bot-application",
      "url": "https://yourbotapp.azurewebsites.net/api/google"
    }
```

3. Save your completed action package in a location of your choice.

4. You now need to update your action using the **gactions** cli tool you downloaded earlier.  Open a terminal and navigate to the location that you saved the **gactions** cli tool into earlier.  You will now also require the command you made a note of earlier when creating your project.  Enter this command into the terminal window, replacing **PACKAGE_NAME** with the location of your recently saved action package.  Your command should look something like the following,

```
gactions update --action_package C:\your-action-package.json --project YOUR-PROJECT-ID
```

5. Execute the command, following the instructions to authenticate with your Google account.  Once completed 


### Test your Google action

You can now test interacting with your action using the simulator. 

1. In the action dashboard navigate to the **Test** tab at the top of the page.

2. To perform a basic test enter "ask <ACTION DISPLAY NAME> hello world" into the simulator input box. For example, if your action display name was 'Adapter Helper', you would type 'Talk to Adapter Helper hello world'. This should return an echo of your message.

![Simulator](/libraries/Bot.Builder.Community.Adapters.Google/media/simulator-test.PNG?raw=true)

Now that you have enabled testing for your action, you can also test your action using a physical Google assistant device or using Google assistant on an Android device. Providing you are logged into the device with the same account used to login to the Actions on Google Console (or an account that you have added as a beta tester for your action within the console).

### Customising your conversation

#### Controlling the end of a session

By default, the Google adapter is configured to close the session following sending a response. You can explicitly indicate that Google should wait for the user to say something else, meaning Google should leave the microphone open and listen for further input, by sending an input hint of ***ExpectingInput*** on your outgoing activity.

```cs
await turnContext.SendActivityAsync("Your message text", inputHint: InputHints.ExpectingInput);
```

You can alter the default behavior to leave the session open and listen for further input by default by setting the ***ShouldEndSessionByDefault*** setting on the ***GoogleAdapterOptions*** class within your startup.cs class.

```csharp
    // Create GoogleAdapterOptions
    services.AddSingleton(sp =>
    {
        return new GoogleAdapterOptions()
        {
            ActionInvocationName = "YOUR-ACTION-DISPLAY-NAME",
            ActionProjectId = "YOUR-PROJECT-ID",
            ShouldEndSessionByDefault = false
        };
    });
```

If you do set ***ShouldEndSessionByDefault*** to false, then you need to explicity end the conversation when you are ready, by sending an input hint of ***IgnoringInput*** on your last outgoing activity.

```cs
await turnContext.SendActivityAsync("Your message text", inputHint: InputHints.IgnoringInput);
```

#### Sending a basic Google card as part of your response

You can include a basic Google card in your response, which is shown on devices that have a screen.  To do this you include an attachment on your outgoing activity.  For more information about basic cards see [https://developers.google.com/assistant/conversational/responses#basic_card](https://developers.google.com/assistant/conversational/responses#basic_card)

```cs
var activityWithCard = MessageFactory.Text($"Ok, I included a simple card.");
                    activityWithCard.Attachments.Add(
                        new BasicCardAttachment(
                            new Adapters.Google.BasicCard()
                            {
                                Content = new BasicCardContent()
                                {

                                    Title = "This is a simple card",
                                    Subtitle = "This is a simple card subtitle",
                                    FormattedText = "This is the simple card content"
                                }
                            }));
                    await turnContext.SendActivityAsync(activityWithCard, cancellationToken);
```

#### Providing a user an interactive list / carousel

In order to provide a user with a list or carousel, from which they can choose an item using their screen, you can add a ListAttachment to your outgoing activity, setting the **ListAttachmentStyle** to determine if a List or Carousel should be used.

More information about Lists and Carousels can be found at [https://developers.google.com/assistant/conversational/responses#visual_selection_responses](https://developers.google.com/assistant/conversational/responses#visual_selection_responses).

Below shows an example of sending a list.

```cs
var activityWithListAttachment = MessageFactory.Text($"Ok, I included a list.");
                    var listAttachment = new ListAttachment(
                        "This is the list title",
                        new List<OptionItem>() {
                            new OptionItem() {
                                Title = "List item 1",
                                Description = "This is the List Item 1 description",
                                Image = new OptionItemImage() { AccessibilityText = "Item 1 image", Url = "https://storage.googleapis.com/actionsresources/logo_assistant_2x_64dp.PNG"},
                                OptionInfo = new OptionItemInfo() { Key = "Item1", Synonyms = new List<string>(){ "first" } }
                            },
                        new OptionItem() {
                                Title = "List item 2",
                                Description = "This is the List Item 2 description",
                                Image = new OptionItemImage() { AccessibilityText = "Item 1 image", Url = "https://storage.googleapis.com/actionsresources/logo_assistant_2x_64dp.PNG"},
                                OptionInfo = new OptionItemInfo() { Key = "Item2", Synonyms = new List<string>(){ "second" } }
                            }
                        },
                        ListAttachmentStyle.List);
                    activityWithListAttachment.Attachments.Add(listAttachment);
                    await turnContext.SendActivityAsync(activityWithListAttachment, cancellationToken);
```

#### Sending a table card

You can include information formatted into a table using the Table Card. See [https://developers.google.com/assistant/conversational/responses#table_cards](https://developers.google.com/assistant/conversational/responses#basic_card) for more information about Table Cards.

```cs
var activityWithTableCardAttachment = MessageFactory.Text($"Ok, I included a table card.");
                    var tableCardAttachment = new TableCardAttachment(
                        new TableCard()
                        {
                            Content = new TableCardContent()
                            {
                                ColumnProperties = new List<ColumnProperties>()
                                {
                                    new ColumnProperties() { Header = "Column 1" },
                                    new ColumnProperties() { Header = "Column 2" }
                                },
                                Rows = new List<Row>()
                                {
                                    new Row() {
                                        Cells = new List<Cell>
                                        {
                                            new Cell { Text = "Row 1, Item 1" },
                                            new Cell { Text = "Row 1, Item 2" }
                                        }
                                    },
                                    new Row() {
                                        Cells = new List<Cell>
                                        {
                                            new Cell { Text = "Row 2, Item 1" },
                                            new Cell { Text = "Row 2, Item 2" }
                                        }
                                    }
                                }
                            }
                        });
                    activityWithTableCardAttachment.Attachments.Add(tableCardAttachment);
                    await turnContext.SendActivityAsync(activityWithTableCardAttachment, cancellationToken);
```
