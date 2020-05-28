---
category: Virtual Assistant
subcategory: Handbook
title: Exchanging data with Skills
description: Exchange data to and from Skills using the SkillDialog
order: 12
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

There are situations when it's helpful to pass data between Skills. Information can be provided to a Skill to perform slot-filling therefore limiting the interactions the user has to perform (e.g. share the current location). Additionally, a Skill can interact with the user through responses as usual but also return data back to the underlying caller which can be used for other purposes. For example, a Virtual Assistant could invoke the Calendar, ToDo and Weather Skill to retrieve information and generate a "Your Day Today" card experience bringing together disparate information. These `action` interactions could be silent to the end user with data being returned from each interaction or be interactive depending on your scenario.

Bot Framework Skills provides the capability to pass data to a Skill through the `Value` property on the Activity sent to the Skill through the SkillDialog. Conversely, when a Skill ends a dialog using `EndDialogAsync` an object can be returned which is marshalled back to the caller for use. You can set this Value property in any-way you desire but an example end to end flow is shown below to guide next steps.

> Action invocation is supported by Bot Framework based Bots including Virtual Assistant along with Power Virtual Agents.

## Pre and Post Processing

In order to pass data to a Skill and process data returned from a Skill, one technique is to create a `Pre` and `Post` waterfall step for each Skill you wish to invoke, an example of this is shown below.

```csharp
 var skillSteps = new WaterfallStep[]
            {
                PreSkillStepAsync,
                PostSkillStepAsync,
            };

  AddDialog(new WaterfallDialog("WeatherActionInvoke", skillSteps));
```

You can then invoke this Skill by starting the Waterfall dialog:

```csharp
    return await innerDc.BeginDialogAsync("WeatherActionInvoke");
```

## Sending data to a Skill

In the `Pre` processing step you can pass data to the Skill by populating the `Value` property on the Activity with the object you wish to serialize and pass to the Skill. The example below, shows an `Action` within the Skill called `WeatherForecast` being invoked and location information being passed. This activity is then passed to the SkillDialog which will process and send across to the skill.

```csharp
    private async Task<DialogTurnResult> PreSkillStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var evt = stepContext.Context.Activity.AsEventActivity();
        if (evt != null)
        {
            LocationInfo location = new LocationInfo();
            location.Location = "51.4644018,-2.1177246,14";

            var activity = (Activity)Activity.CreateEventActivity();
            activity.Name = "WeatherForecast";
            activity.Value = location;

            // Create the BeginSkillDialogOptions
            var skillDialogArgs = new BeginSkillDialogOptions { Activity = activity };

            // Start the skillDialog instance with the arguments. 
            return await stepContext.BeginDialogAsync("WeatherSkill", skillDialogArgs, cancellationToken);
        }

        return await stepContext.NextAsync();
    }
```

## Retrieving data after a Skill interaction

The `Post` processing step will be invoked once the Skill processing has been completed. If data has been returned you will find this within the `stepContext.Result` property.

```csharp
    private async Task<DialogTurnResult> PostSkillStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        // stepContext.Result has any returning data from a Skill
        if (stepContext.Result != null)
        {
            var returnObject = JsonConvert.SerializeObject(stepContext.Result);
            // Perform your processing here
        }

        return await stepContext.NextAsync();
    }
```

## Retrieving data within a Skill

Within your Skill, you then need to handle the Event triggered by the previous steps to retrieve the data and start dialog processing as usual. With the Virtual Assistant and Skill Template this would be within your `RouteStepAsync` method. The example below shows handling the `WeatherForecast` event used above and retrieving data from the `Value` property of an activity. You could then populate the state object with information used by downstream dialogs. An example Action is provided with the Skill Template and you can review the implementation [here](https://github.com/microsoft/botframework-solutions/blob/master/samples/csharp/skill/SkillSample/Dialogs/MainDialog.cs#L245.)

```csharp
case "WeatherForecast":
{
    LocationInfo locationData = null;
    if (ev.Value is JObject location)
    {
        locationData = location.ToObject<LocationInfo>();
        // Process data here
    }

    // Start a dialog to process..
    return await stepContext.BeginDialogAsync(YOUR_DIALOG.id, options);
```

## Returning data back to the caller from a Skill

Finally, once a Skill has finished processing it can optionally decide to return supporting data to the caller through the `result` parameter on `EndDialogAsync`. You have complete control over the structure of the returned object. In this example the forecast data is returned to the caller which can make use of it as required. 

```csharp
    return await sc.EndDialogAsync(new WeatherForecastInformation { Forecast = forecast });
```

## Summary

Exchanging data to and from Skills is an optional, but powerful way to build proactive and reactive experiences including those that aggregate data from a variety of Skills to create a more unified experience. 
