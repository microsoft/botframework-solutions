---
layout: tutorial
category: Skills
subcategory: Add action support
language: csharp
title: Add action handling code
order: 2
---

# Tutorial: {{page.subcategory}} ({{page.language}})

## {{ page.title }}

### Purpose

In addition to utterance based invocation of your Skill, we have now introduced action based invocation similar to a method call whereby a client can invoke a specific function of your Skill passing data (slots) and receive response data back. The Skill can still prompt as usual in a conversational manner for missing slots and other needs.

Follow this part of the tutorial to wire up the action you created as part of the previous step.

### Steps

1. Create a new class called to represent the new Action within your Dialogs folder and paste in the example class definition shown below. This class is based on the existing `SampleAction` and shows some extensions to handle an incoming object containing data and how to return an object back to the caller as part the end dialog operation.

    Note that action processing can send activities just like any Dialog and prompt for missing data or confirmation. Update the input and output types as per the previous step along with validation steps.

    Note that an output object can be returned to a caller by passing it on the call to `EndDialogAsync`. This is then automatically added to the `EndOfConversation` event and accessible by the caller.

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
            IServiceProvider serviceProvider)
            : base(nameof(SampleAction), serviceProvider)
        {
            var sample = new WaterfallStep[]
            {
                PromptForNameAsync,
                GreetUserAsync,
                EndAsync,
            };

            AddDialog(new WaterfallDialog(nameof(SampleAction), sample));
            AddDialog(new TextPrompt(DialogIds.NamePrompt));

            InitialDialogId = nameof(SampleAction);
        }

        private async Task<DialogTurnResult> PromptForNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If we have been provided a input data structure we pull out provided data as appropriate
            // and make a decision on whether the dialog needs to prompt for anything.
            if (stepContext.Options is SampleActionInput actionInput && !string.IsNullOrEmpty(actionInput.Name))
            {
                // We have Name provided by the caller so we skip the Name prompt.
                return await stepContext.NextAsync(actionInput.Name, cancellationToken);
            }

            var prompt = TemplateEngine.GenerateActivityForLocale("NamePrompt");
            return await stepContext.PromptAsync(DialogIds.NamePrompt, new PromptOptions { Prompt = prompt }, cancellationToken);
        }

        private async Task<DialogTurnResult> GreetUserAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            dynamic data = new { Name = stepContext.Result.ToString() };
            var response = TemplateEngine.GenerateActivityForLocale("HaveNameMessage", data);
            await stepContext.Context.SendActivityAsync(response);

            // Pass the response which we'll return to the user onto the next step
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> EndAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Simulate a response object payload
            var actionResponse = new SampleActionOutput
            {
                CustomerId = new Random().Next()
            };

            // We end the dialog (generating an EndOfConversation event) which will serialize the result object in the Value field of the Activity
            return await stepContext.EndDialogAsync(actionResponse, cancellationToken);
        }

        private static class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
    ```

1. Add the following line to your `Startup.cs` class to make the new action available for use.

    ```csharp
    services.AddTransient<SampleAction>();
    ```

1. Within your `MainDialog.cs` class you need to add the newly created dialog so it's available for use. Add this line to your `MainDialog` constructor whilst also creating a local variable.

    ```csharp
    // Register dialogs
    _sampleAction = serviceProvider.GetService<SampleAction>();
    AddDialog(_sampleAction);
    ```

1. Within your `MainDialog.cs` class you need to manage a handler for each incoming Action your Skill supports. The name of your action specified within the `name` property of your Action definition is mapped to the `Name` property of the incoming activity.

    ```csharp
    private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var activity = stepContext.Context.Activity;
        if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
        {
            ...
        }
        else if (activity.Type == ActivityTypes.Event)
        {
            var ev = activity.AsEventActivity();

            if (!string.IsNullOrEmpty(ev.Name))
            {
                switch (ev.Name)
                {
                    case "SampleAction":
                        {
                            SampleActionInput actionData = null;

                            if (ev.Value is JObject eventValue)
                            {
                                actionData = eventValue.ToObject<SampleActionInput>();
                            }

                            // Invoke the SampleAction dialog passing input data if available
                            return await stepContext.BeginDialogAsync(nameof(SampleAction), actionData, cancellationToken);
                        }

                    default:
                        {
                            await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."), cancellationToken);
                            break;
                        }
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: "An event with no name was received but not processed."), cancellationToken);
            }
        }
    }
    ```

1. Build and deploy your updated Skill. You have now added an additional Action to your Skill which should be visible within a client application such as Power Virtual Agent.
