---
category: Reference
subcategory: Skills
title: Responses
description: Details on how responses work in skill projects.
order: 6
---

# {{ page.title }}
{:.no_toc}

## In this reference
{:.no_toc}

* 
{:toc}


## Response Files
To configure responses for your skill project, you'll need a `.json` file and a text template file (`.tt`) for each collection of responses. An example of each can be found in the Skill Template project in the Responses folder.

### Json Structure
Responses can be stored in the following format to be used in your project. Each `.json` file should have a Build Action of **EmbeddedResource** to be loaded properly at runtime. 
```
"templateName": {
  "replies": [
    {
      "text": "Welcome to my bot, {Name}!",
      "speak": "Welcome, {Name}!"
    }
  ],
  "suggestedActions": [
    "Help"
  ],
  "inputHint": "acceptingInput"
}
```

| Property | Description |
| -------- | ----------- |
| templateName | The name used to call the template |
| replies | The collection of replies. A random reply will be selected when called |
| text  | The text that will be shown in the response. Tokens denoted with `{}` can be replaced by the [Response Manager](#response-manager). |
| speak | The text that will be spoken with the response. Tokens denoted with `{}` can be replaced by the [Response Manager](#response-manager). |
| inputHint | Indicates mic configuration for the response. Values are *acceptingInput* (default), *expectingInput* (for prompts), *ignoringInput* (intermediate responses)|
| suggestedActions (optional) | Sets simple suggestedActions on the response. |

#### Localization
To provide localized versions of responses, add additional `.json` files for each language. The file name format should be name.locale.json (e.g. `MyResponses.de.json`).


### Text Template
The text template (`.tt`) file auto-generates a class representing the names of the responses in the `.json` file. This allows you to reference the responses names more easily. The text template and `.json` files should have the same root name and be in the same folder (e.g. `MyResponses.tt` and `MyResponses.json`).

```csharp
namespace SkillSample.Responses.Sample
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class SampleResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string NamePrompt = "NamePrompt";
        public const string HaveNameMessage = "HaveNameMessage";
    }
}
```

## Adaptive Cards
Adaptive cards can be added in `.json` format to the Content folder to be accessed throughout your project. Each `.json` file should have a Build Action of EmbeddedResource to be loaded properly at runtime. 

### Card Object

Adaptive cards are referenced via the Card type. To create a Card object from the Adaptive Card *MyCard.json*, use the following code:

```csharp
new Card("MyCard")
```

### ICardData Interface
The ICardData interface is used to replace tokens in your Adaptive Card. This allows you to provide different values into different containers in your card.

If *MyCard.json* contains that following Adaptive TextBlock:
```json
{
  "type": "TextBlock",
  "text": "{Name}",
  "weight": "bolder",
  "wrap": true
}
```

Given the following ICardData implementation:

```csharp
public class MyCardData : ICardData
{
    public MyCardData(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}
```
The following will generate a card that replaces the `{Name}` token with the provided value, "Jane".
```csharp
new Card("MyCard", new MyCardData("Jane"))
```

## Response Manager
The ResponseManager class loads each response file that is specified into a collection of responses for your bot. It is initialized in `Startup.cs` and can be used throughout the project.

## Initalizing the ResponseManager
In `Startup.cs`, the following code initializes your ResponseManager object with the specified locales and your response files:

```csharp
// Configure responses
services.AddSingleton(sp => new ResponseManager(
	new[] { "en", "de", "es" }, // array of supported locales
	new MainResponses(), // Auto-generated object created by text template
	new SharedResponses(),
	new SampleResponses()));
```

## Using the ResponseManager
Once you have created your `.json` and `.tt` files and initialized your ResponseManager in `Startup.cs`, you can use the response manager to build your responses. The following methods and overloads are available to you.

### GetResponse()

- Get a simple response from template with Text, Speak, InputHint, and SuggestedActions set.

    ```csharp
    GetResponse(string templateId, StringDictionary tokens = null)
    ```

    | Parameter | Description |
    | --------- | ----------- |
    | templateId | The name of the response template. |
    | tokens | StringDictionary of tokens to replace in the response. |

### GetCardResponse()

- Get a response with an Adaptive Card attachment.
  
    ```csharp
    GetCardResponse(Card card)
    ```

    | Parameter | Description |
    | --------- | ----------- |
    | card | The [Card](#card-object) object to add to the response. |

- Get a response with a list of Adaptive Card attachments.

    ```csharp
    GetCardResponse(
        IEnumerable<Card> cards,
        string attachmentLayout = AttachmentLayoutTypes.Carousel)
    ```

    | Parameter | Description |
    | --------- | ----------- |
    | cards | A list of [Card](#card-object) objects to add to the response. |
    | attachmentLayout | The attachment layout for the resulting activity. Carousel or List |

- Get a response from template with Text, Speak, InputHint, SuggestedActions, and an Adaptive Card attachment.
    ```csharp
    GetCardResponse(
        string templateId,
        Card card,
        StringDictionary tokens = null)
    ```
    | Parameter | Description |
    | --------- | ----------- |
    | templateId | The name of the response template. |
    | card | The [Card](#card-object) object to add to the response. |
    | tokens | StringDictionary of tokens to replace in the response. |

- Get a response from template with Text, Speak, InputHint, SuggestedActions, and a list of Adaptive Card attachments.
    ```csharp
    GetCardResponse(
        string templateId,
        IEnumerable<Card> cards,
        StringDictionary tokens = null,
        string attachmentLayout = attachmentLayoutTypes.Carousel)
    ```

    | Parameter | Description |
    | --------- | ----------- |
    | templateId | The name of the response template. |
    | cards | A list of [Card](#card-object) objects to add to the response. |
    | tokens | StringDictionary of tokens to replace in the response. |
    | attachmentLayout | The attachment layout for the resulting activity. Carousel or List |

- Get a response from template with Text, Speak, InputHint, SuggestedActions, and a Card attachments with list items inside.
    ```csharp
    GetCardResponse(
        string templateId,
        Card card,
        StringDictionary tokens = null,
        string containerName = null,
        IEnumerable<Card> containerItems = null)
    ```

    | Parameter | Description |
    | --------- | ----------- |
    | templateId | The name of the response template. |
    | card |  The main [Card](#card-object) object containing a list. |
    | tokens | StringDictionary of tokens to replace in the response. |
    | containerName | Target container where list of containerItems will be inserted. |
    | containerItems | List of card objects to be inserted into target container. |