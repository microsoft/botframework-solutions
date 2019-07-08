# Adding Your Virtual Assistant to Microsoft Teams

**APPLIES TO:** ✅ SDK v4

## In this tutorial
- [Intro](#intro)
- [Add the Microsoft Teams Channel](#Add-the-Microsoft-Teams-Channel)
- [Installing App Studio](#Installing-App-Studio)
- [Create the Application Manifest for Teams](#Create-the-Application-Manifest-for-Teams)
- [Testing within Teams](#Testing-within-Teams)
- [Adding Commands](#Adding-Commands)
- [Next Steps](#Next-Steps)

## Intro

### Purpose

The Virtual Assistant template creates and deploys an Assistant with all required integration steps for Microsoft Teams. 

This tutorial covers the steps required to connect your Virtual Assistant to Microsoft Teams and creation of an application manifest required to install your assistant within Teams.

### Prerequisites

- [Create a Virtual Assistant](/docs/tutorials/csharp/virtualassistant.md) to setup your environment.

- Microsoft Teams installed and configured to work with your Office 365 tenant.

### Time to Complete

10 minutes

### Scenario

Add your Virtual Assistant to Teams and creation of a manifest for installation of your assistant.

## Add the Microsoft Teams Channel

The first step is to connect your deployed Bot to the Microsoft Teams channel.

1. Go to the Azure Portal and locate the Web App Bot created for your Assistant which is most easily found by opening the Resource Group.
2. Click `Channels` on the left-hand navigation and select `Microsoft Teams`
3. Click `Save` to add the Channel to your Virtual Assistant.

## Installing App Studio

The next step is to create an Application Manifest for Teams. The most significant part of a Microsoft Teams app package is its manifest.json file. This file, which must conform to the Teams App schema, contains metadata which allows Teams to correctly present your app to users.

The Manifest Editor tab in App Studio simplifies creating the manifest, allowing you to describe the app, upload your icons, add app capabilities, and produce a .zip file which can easily be uploaded into Teams for testing or distributed for others to use. This manifest references your deployed Bot endpoint.

1. App Studio is a Teams app which can be found in the Teams store. See the Store Icon in the left-hand ribbon of Teams, or follow [this link for direct download](https://aka.ms/InstallTeamsAppStudio).
2. Select the App Studio tile to open the app install page and click Install.

    ![App Install Page](/docs/media/teamsappstudioconfiguration.png)

## Create the Application Manifest for Teams

1. Open App Studio and click Manifest Editor from the top menu bar.
2. Choose `Create a New App`
3. Fill in the first page of the Application Manifest with the information related to your Application. Note that the App ID referenced does not related to the Application ID of your deployed Virtual Assistant.

    ![New Manifest](/docs/media/teamsnewmanifestpage.png)
4. Click Bots in the left-hand navigation of App Studio and click `Set up`
5. Choose `Existing Bot` from the `Set up a Bot` window
6. Enter a name for your Bot and retrieve the `microsoftAppId` from the appSettings.json file located in your Assistant project directory and paste into the text-box under `Connect to a different bot id`. Then select `Personal` as the scope
   
   ![Manifest Setup Bot](/docs/media/teamsnewmanifestsetupbot.png)
7. Click `Create Bot`
8. Now Click `Domains and Permissions` within App Studio and add `token.botframework.com` to Valid Domains. This is required for any Authentication steps you have within your Virtual Assistant or Teams to work correctly.
9. Finally, you can now click `Test and Distribute` to install your Virtual Assistant for testing or Download the Manifest zip file for distribution. At this time, click Install for local testing.

   ![Manifest Test](/docs/media/teamsnewmanifesttest.png)

## Testing within Teams

1. After you have clicked Install in the previous step you should now see your Bot available within the `Chat` section of Teams.
2. You should now be able to talk to your Virtual Assistant and any configured Skills as you would do in any other channel.

## Adding Commands

An optional additional step is to add example commands that will help users understand what features your Assistant can perform. An example of this is shown below.

![Manifest Test](/docs/media/teamscommandexample.png)

1. To add these, go back into the Manifest Editor and open the Application you previously created.
2. Click `Bots` in the left hand navigation.
3. Click `Add` under the Command section

    ![Manifest Test](/docs/media/teamsnewmanifestcommands.png)
4. Provide the utterance that Teams should send to your Assistant in the `Command text` box. A more friendly help text can then be provided in the `Help text` box. Choose `Personal` to only show this in 1:1 conversations.

    ![Manifest Test](/docs/media/teamsaddcommand.png)
5. Click Save and repeat for any other commands you wish to add.
6. Navigate to `Test and Distribute` to install your Virtual Assistant for testing or Download the Manifest zip file for distribution. You can click `Install` to repeat local testing.

## Next Steps

The Microsoft Teams documentation has additional documentation around Microsoft Teams and Bots with two key items highlighted below.

- [Test and debug your Microsoft Teams bot](https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/bots/bots-test)
- [Quickly develop apps with App Studio for Microsoft Teams](https://docs.microsoft.com/en-us/microsoftteams/platform/get-started/get-started-app-studio)


 

