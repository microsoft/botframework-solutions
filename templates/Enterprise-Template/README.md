# Conversational AI - Enterprise Template

Creation of a high quality conversational experience requires a foundational set of capabilities. To help you succeed with building great conversational experiences, we have created an Enterprise Bot Template. This template brings together all of the best practices and supporting components we've identified through building of conversational experiences.

This template greatly simplifies the creation of a new bot project. The template will provide the following out of box capabilities, leveraging [Bot Builder SDK v4](https://github.com/Microsoft/botbuilder) and [Bot Builder Tools](https://github.com/Microsoft/botbuilder-tools).

-   Introduction message with an Adaptive Card on conversation start. It explains the bot's capabilities and provides buttons to guide initial questions. Developers can then customize this as appropriate.
-   Automated typing indicators
-   .bot file driven configuration
-   Basic conversational intents (Greeting, Goodbye, Help, Cancel, etc.) in English, French, Italian, German, Spanish. These are provided in .LU (language understanding) files enabling easy modification.
-   Example responses to basic conversational intents abstracted into separate View classes. These will move to the new language generation (LG) files in the future.
-   Inappropriate content or PII (personally identifiable information) detection in incoming conversations through use of [Content Moderator](https://azure.microsoft.com/en-us/services/cognitive-services/content-moderator/)  in a middleware component.
-   Transcripts of all converstartion stored in Azure Storage
-   An integrated [Dispatch](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-dispatch?view=azure-bot-service-4.0&tabs=csaddref%2Ccsbotconfig) model to identify whether a given utterance should be processed by LUIS + Code or passed to QnAMaker.
-   Integration with [QnAMaker](https://www.qnamaker.ai) to answer general questions
-   Integration with [Application Insights](https://azure.microsoft.com/en-gb/services/application-insights/) to collect telemetry for all conversations and an example PowerBI dashboard to get you started with insights into your conversational experiences.

In addition, all of the Azure resources required for the Bot are automatically deployed: Bot registration, Azure App Service, LUIS, QnAMaker, Content Moderator, CosmosDB, Azure Storage, and Application Insights. Additionally, base LUIS, QnAMaker, and Dispatch models are created, trained, and published to enable immediate testing of basic intents and routing.

Once the template is created and deployment steps are executed you can hit F5 to test end-to-end. This provides a solid base from which to start your conversational experience, reducing multiple days' worth of effort that each project had to undertake and raises the conversational quality bar.

## Getting Started

- Refer to the [main documentation](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-enterprise-template-create-project?view=azure-bot-service-4.0
) to get started with the Enterprise Template which is avialable for both Visual Studio and Yeoman.
- This repo contains the Source Code for the template which will continue to be evolved with the latest best practice and capabilities. Feel free to raise Issues or submit Pull Requests.

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
