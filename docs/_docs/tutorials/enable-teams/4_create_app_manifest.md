---
category: Tutorials
subcategory: Enable Microsoft Teams
language: csharp javascript
title: Create application manifest
order: 4
---

# Tutorial: Adding your Assistant to Microsoft Teams

## Create the Application Manifest for Teams

1. Open App Studio and click Manifest Editor from the top menu bar.
2. Choose `Create a New App`
3. Fill in the first page of the Application Manifest with the information related to your Application. Note that the App ID referenced does not related to the Application ID of your deployed Virtual Assistant.

    ![New Manifest]({{site.baseurl}}/assets/images/teamsnewmanifestpage.png)
4. Click Bots in the left-hand navigation of App Studio and click `Set up`
5. Choose `Existing Bot` from the `Set up a Bot` window
6. Enter a name for your Bot and retrieve the `microsoftAppId` from the appSettings.json file located in your Assistant project directory and paste into the text-box under `Connect to a different bot id`. Then select `Personal` as the scope
   
   ![Manifest Setup Bot]({{site.baseurl}}/assets/images/teamsnewmanifestsetupbot.png)
7. Click `Create Bot`
8. Now Click `Domains and Permissions` within App Studio and add `token.botframework.com` to Valid Domains. This is required for any Authentication steps you have within your Virtual Assistant or Teams to work correctly.
9. Finally, you can now click `Test and Distribute` to install your Virtual Assistant for testing or Download the Manifest zip file for distribution. At this time, click Install for local testing.

   ![Manifest Test]({{site.baseurl}}/assets/images/teamsnewmanifesttest.png)