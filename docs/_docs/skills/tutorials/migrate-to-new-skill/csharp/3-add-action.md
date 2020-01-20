---
layout: tutorial
category: Skills
subcategory: Migrate to GA Bot Framework Skills
language: C#
title: Add action handling code
order: 1
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

### Purpose

In addition to utterance based invocation of your Skill, we have now introduced action based invocation similar to a method call whereby a client can invoke a specific function of your Skill passing data (slots) and receive response data back. The skill can still prompt as usual in a conversational manner for missing slots and other needs.

Follow this part of the tutorial if you wish to extend your Skill to add action capabilities.

### Steps

1. Create a new class called `SampleAction` within your Dialogs folder and paste in the class definitions shown below. This class is based on the existing `SampleDialog` and shows some extensions to handle an incoming object containing data and how to return an object back to the caller as part the end dialog operation.

Note that action processing can send activities just like any Dialog and prompt for missing data or confirmation.

```csharp
    public class SampleActionInput
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class SampleActionOutput
    {
        [JsonProperty("customerId")]
        public int CustomerId { get; set; }
    }

    public class SampleAction : SkillDialogBase
    {
        public SampleAction(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(SampleAction), serviceProvider, telemetryClient)
        {
            var sample = new WaterfallStep[]
            {
                PromptForName,
                GreetUser,
                End,
            };

            AddDialog(new WaterfallDialog(nameof(SampleAction), sample));
            AddDialog(new TextPrompt(DialogIds.NamePrompt));

            InitialDialogId = nameof(SampleAction);
        }

        private async Task<DialogTurnResult> PromptForName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If we have been provided a input data structure we pull out provided data as appropriate
            // and make a decision on whether the dialog needs to prompt for anything.
            var actionInput = stepContext.Options as SampleActionInput;
            if (actionInput != null && !string.IsNullOrEmpty(actionInput.Name))
            {
                // We have Name provided by the caller so we skip the Name prompt.
                return await stepContext.NextAsync(actionInput.Name);
            }

            var prompt = TemplateEngine.GenerateActivityForLocale("NamePrompt");
            return await stepContext.PromptAsync(DialogIds.NamePrompt, new PromptOptions { Prompt = prompt });
        }

        private async Task<DialogTurnResult> GreetUser(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            dynamic data = new { Name = stepContext.Result.ToString() };
            var response = TemplateEngine.GenerateActivityForLocale("HaveNameMessage", data);
            await stepContext.Context.SendActivityAsync(response);

            // Pass the response which we'll return to the user onto the next step
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Simulate a response object payload
            SampleActionOutput actionResponse = new SampleActionOutput();
            actionResponse.CustomerId = new Random().Next();

            // We end the dialog (generating an EndOfConversation event) which will serialize the result object in the Value field of the Activity
            return await stepContext.EndDialogAsync(actionResponse);
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
```

1. Add the following line to your Startup.cs class to make the new action available for use.

```csharp
    services.AddTransient<SampleAction>();
```

1. Within your `MainDialog` class you need to add the newly created dialog so it's available for use. Add this line to your MainDialog constructor whilst also creating a local variable.

```csharp
    // Register dialogs
    _sampleAction = serviceProvider.GetService<SampleAction>();
    AddDialog(_sampleAction);
```

1. Within your `MainDialog` class you need to handle a handler for each incoming Action your Skill supports. As per the example Manifest in Step 2, an action called `SampleAction` was defined which will result in an Event Activity being received by teh Skill. In the example below any input data is retrieved from the `Value` property of the Activity and passed into the dialog.

```csharp
protected override async Task OnEventActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken)
{
    var eventActivity = dc.Context.Activity.AsEventActivity();

    if (!string.IsNullOrEmpty(eventActivity.Name))
    {
        switch (eventActivity.Name)
        {
            // Each Action in the Manifest will have an associated Name which will be on incoming Event activities
            case "SampleAction":

                SampleActionInput actionData = null;                       

                var eventValue = dc.Context.Activity.Value as JObject;                      
                if (eventValue != null)
                {
                    actionData = eventValue.ToObject<SampleActionInput>();                            
                }

                // Invoke the SampleAction dialog passing input data if available
                await dc.BeginDialogAsync(nameof(SampleAction), actionData);

                break;

            default:

                await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{eventActivity.Name ?? "undefined"}' was received but not processed."));

                break;
        }
    }
    else
    {
        await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"An event with no name was received but not processed.")); 
    }
}
```

