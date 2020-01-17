---
layout: tutorial
category: Clients and Channels
subcategory: Extend to Microsoft Teams
title: Install App Studio
order: 3
---

# Tutorial: {{page.subcategory}}

## Installing App Studio

The next step is to create an Application Manifest for Teams. The most significant part of a Microsoft Teams app package is its manifest.json file. This file, which must conform to the Teams App schema, contains metadata which allows Teams to correctly present your app to users.

The Manifest Editor tab in App Studio simplifies creating the manifest, allowing you to describe the app, upload your icons, add app capabilities, and produce a .zip file which can easily be uploaded into Teams for testing or distributed for others to use. This manifest references your deployed Bot endpoint.

1. App Studio is a Teams app which can be found in the Teams store. See the Store Icon in the left-hand ribbon of Teams, or follow [this link for direct download](https://aka.ms/InstallTeamsAppStudio).
1. Select the App Studio tile to open the app install page and click Install.

    ![App Install Page]({{site.baseurl}}/assets/images/teamsappstudioconfiguration.png)