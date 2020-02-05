---
layout: tutorial
category: Clients and Channels
subcategory: Extend to Microsoft Teams
title: Add commands
order: 6
---

# Tutorial: {{page.subcategory}}

## Adding Commands

An optional additional step is to add example commands that will help users understand what features your Assistant can perform. An example of this is shown below.

![Manifest Test]({{site.baseurl}}/assets/images/teamscommandexample.png)

1. To add these, go back into the Manifest Editor and open the Application you previously created.
1. Click `Bots` in the left hand navigation.
1. Click `Add` under the Command section

    ![Manifest Test]({{site.baseurl}}/assets/images/teamsnewmanifestcommands.png)
1. Provide the utterance that Teams should send to your Assistant in the `Command text` box. A more friendly help text can then be provided in the `Help text` box. Choose `Personal` to only show this in 1:1 conversations.

    ![Manifest Test]({{site.baseurl}}/assets/images/teamsaddcommand.png)
1. Click Save and repeat for any other commands you wish to add.
1. Navigate to `Test and Distribute` to install your Virtual Assistant for testing or Download the Manifest zip file for distribution. You can click `Install` to repeat local testing.
