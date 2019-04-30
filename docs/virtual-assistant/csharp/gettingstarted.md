# Getting Started With the Virtual Assistant (C#)

> [!NOTE]
> This topics applies to v4 version of the SDK.

## Table of Contents
- [Getting Started With the Virtual Assistant (C#)](#getting-started-with-the-virtual-assistant-c)
  - [Table of Contents](#table-of-contents)
  - [Prerequisites](#prerequisites)
  - [Create your project](#create-your-project)
  - [Deployment](#deployment)
  - [Testing](#testing)
  
## Prerequisites
> It's important to ensure all of the following pre-requisites are installed on your machine prior to attempting deployment otherwise you may run into deployment issues.

1. Install the [Virtual Assistant Template](https://botbuilder.myget.org/gallery/aitemplates)
2. Ensure you have updated [.NET Core](https://www.microsoft.com/net/download) to the latest version.  
3. Ensure the [Node Package manager](https://nodejs.org/en/) is installed.
4. PowerShell Core version 6 (Required for cross platform deployment support)
   * [Download PowerShell Core on Windows](https://aka.ms/getps6-windows)
   * [Download PowerShell Core on macOS and Linux](https://aka.ms/getps6-linux)
5. Install  Bot Framework (CLI) tool dependencies. It's important to do this even if you have earlier versions as the Virtual Assistant makes use of the latest capabilities: 
   ```
   npm install -g botdispatch ludown luis-apis qnamaker luisgen
   ```
6. Install the [Azure Command Line Tools (CLI)](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-windows?view=azure-cli-latest)
7. Retrieve your LUIS Authoring Key
   - Review the [LUIS regions](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-regions) documentation page for the correct LUIS portal for the region you plan to deploy to. Note that www.luis.ai refers to the US region and an authoring key retrieved from this portal will not work within a europe deployment. 
   - Once signed in click on your name in the top right hand corner.
   - Choose Settings and make a note of the Authoring Key for the next step.

## Create your project

1. In Visual Studio, click **File > New Project**.
2. Under Bot, select **Virtual Assistant Template**.
3. Name your project and click **Create**.
4. Click **Tools > NuGet Package Manager > Package Manager Settings**
5. In the Navigation Pane, select **Package Sources**
6. Click '+' to add the following sources: 
   - **https://botbuilder.myget.org/F/experimental/api/v3/index.json**
   - **https://botbuilder.myget.org/F/aitemplates/api/v3/index.json**
9. Click **OK**.
10. Build your project to restore your NuGet packages.

You now have your own Assistant! Follow the Deployment steps below before you try and run the project as deployment creates key dependencies required for operation.

## Deployment

The Virtual Assistant require the following dependencies for end to end operation which are created through an ARM script which you can modify as required.

- Azure Web App
- Azure Storage Account (Transcripts)
- Azure Application Insights (Telemetry)
- Azure CosmosDb (State)
- Azure Cognitive Services - Language Understanding
- Azure Cognitive Services - QnAMaker (including Azure Search, Azure Web App)

> Review the pricing and terms for the services and adjust to suit your scenario.

To deploy your services using the default configuration, follow the steps in this common [deployment documentation page](/docs/virtual-assistant/common/deploymentsteps.md)

## Testing
Once deployment is complete, you can start debugging through the following steps:
- Start a Debugging session within Visual Studio for the Virtual Assistant project
- Open the [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator). 
- Within the Emulator, choose Open Bot from the File menu:
  - Provide the endpoint of your running Bot, e.g: `http://localhost:3978/api/messages`
  - Provide the AppId and Secret values which you can find in your `appsettings.json` file under the `microsoftAppId` and `microsoftAppPassword` configuration settings.

You should see an Introduction Adaptive card as shown below

![Introduction Card](https://user-images.githubusercontent.com/43043272/55245287-0e01fe00-5200-11e9-8709-4d24c0f45502.png)
