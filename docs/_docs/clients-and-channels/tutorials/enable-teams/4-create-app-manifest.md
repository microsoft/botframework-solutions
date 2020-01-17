---
layout: tutorial
category: Clients and Channels
subcategory: Extend to Microsoft Teams
title: Create application manifest
order: 4
---

# Tutorial: {{page.subcategory}}

## Create the Application Manifest for Teams

1. Open App Studio and click Manifest Editor from the top menu bar.
1. Choose `Create a New App`
1. Fill in the first page of the Application Manifest with the information related to your Application. Note that the App ID referenced does not related to the Application ID of your deployed Virtual Assistant.

    ![New Manifest]({{site.baseurl}}/assets/images/teamsnewmanifestpage.png)
1. Click Bots in the left-hand navigation of App Studio and click `Set up`
1. Choose `Existing Bot` from the `Set up a Bot` window
1. Enter a name for your Bot and retrieve the `microsoftAppId` from the appSettings.json file located in your Assistant project directory and paste into the text-box under `Connect to a different bot id`. Then select `Personal` as the scope
   
   ![Manifest Setup Bot]({{site.baseurl}}/assets/images/teamsnewmanifestsetupbot.png)
1. Click `Create Bot`
1. Now Click `Domains and Permissions` within App Studio and add `token.botframework.com` to Valid Domains. This is required for any Authentication steps you have within your Virtual Assistant or Teams to work correctly.
1. Finally, you can now click `Test and Distribute` to install your Virtual Assistant for testing or Download the Manifest zip file for distribution. At this time, click Install for local testing.

   ![Manifest Test]({{site.baseurl}}/assets/images/teamsnewmanifesttest.png)