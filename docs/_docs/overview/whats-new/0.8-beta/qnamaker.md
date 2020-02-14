---
category: Overview
subcategory: What's New
language: 0_8_release
title: QnA Maker updates
date: 2020-02-03
description: Summarizes key updates made to the Virtual Assistant enabling you to easily update parts of your existing Assistant
order: 3
toc: true
---

# Beta Release 0.8
## {{ page.title }}
{:.no_toc}
{{ page.description }}

### QnAMakerDialog

#### Transition to QnAMakerDialog

With the R7 release of the Bot Framework SDK a new `QnAMakerDialog` class was introduced providing key new capabilities which has enabled the Virtual Assistant to transition to this formal SDK capability.

`QnAMakerDialog` introduces support for [Follow-Up prompts](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/multiturn-conversation) and [Active learning](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/improve-knowledge-base) along with use of cards for cases of ambiguity. 

### BotServices

The first change is to move away from creation of `QnAMaker` instances for each QnAMaker knowledgebase as part of the `BotServices` class in your project.

The specific code change is shown below, it uses a new `QnAConfiguration` property and persists the `QnAMakerEndpoint`. This can also be seen [here](https://github.com/microsoft/botframework-solutions/blob/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Services/BotServices.cs) as part of the Sample project.

```csharp
foreach (var kb in config.Knowledgebases)
{
    var qnaEndpoint = new QnAMakerEndpoint()
    {
        KnowledgeBaseId = kb.KbId,
        EndpointKey = kb.EndpointKey,
        Host = kb.Hostname,
    };

    set.QnAConfiguration.Add(kb.Id, qnaEndpoint);
}

CognitiveModelSets.Add(language, set);
```

#### Initialise QnAMakerDialog for each Knowledgebase

The second change is to initialise a QnAMaker dialog for each registered knowledgebase as part of the `MainDialog` constructor.

The specific code change is shown below, this will select the appropriate knowledgebase for the required locale and create a new dialog. Language Generation prompts are used to ensure the QnAMaker dialog uses localised responses for prompts it generates. This can also be seen [here](https://github.com/microsoft/botframework-solutions/blob/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Dialogs/MainDialog.cs#L74) as part of the Sample project.

```csharp
// Register a QnAMakerDialog for each registered knowledgebase and ensure localised responses are provided.
var localizedServices = _services.GetCognitiveModels();
foreach (var knowledgebase in localizedServices.QnAConfiguration)
{
    var qnaDialog = new QnAMakerDialog(
        knowledgeBaseId: knowledgebase.Value.KnowledgeBaseId,
        endpointKey: knowledgebase.Value.EndpointKey,
        hostName: knowledgebase.Value.Host,
        noAnswer: _templateEngine.GenerateActivityForLocale("UnsupportedMessage"),
        activeLearningCardTitle: _templateEngine.GenerateActivityForLocale("QnaMakerAdaptiveLearningCardTitle").Text,
        cardNoMatchText: _templateEngine.GenerateActivityForLocale("QnaMakerNoMatchText").Text)
    {
        Id = knowledgebase.Key
    };
    AddDialog(qnaDialog);
}
```

#### Activating the QnAMakerDialog

The third change is to invoke the appropriate `QnAMakerDialog` when Dispatch indicates QnAMaker should process a given utterance.

The specific code change to `RouteStepAsync` is shown below and can also be seen [here](https://github.com/microsoft/botframework-solutions/blob/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Dialogs/MainDialog.cs#L331) as part of the Sample project.

```csharp
 else if (dispatchIntent == DispatchLuis.Intent.q_Faq)
{
    await innerDc.BeginDialogAsync("Faq");
}
else if (dispatchIntent == DispatchLuis.Intent.q_Chitchat)
{
    innerDc.SuppressCompletionMessage(true);

    await innerDc.BeginDialogAsync("Chitchat");
}
```

#### LG Updates

The final change is adding two additional LG responses to your `MainResponses.lg` file in your Response folder. These are used by the QnAMakerDialog to provide follow-up prompts supporting Active Learning.

These are shown below and can also be seen [here](https://github.com/microsoft/botframework-solutions/blob/master/samples/csharp/assistants/virtual-assistant/VirtualAssistantSample/Responses/MainResponses.lg) as part of the Sample project.

```markdown
# QnaMakerAdaptiveLearningCardTitle
- Did you mean:
- One of these?

# QnaMakerNoMatchText
- None of the above.
- None of these.
```
