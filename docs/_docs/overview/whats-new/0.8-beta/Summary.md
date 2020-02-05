---
category: Overview
subcategory: What's New
language: 0_8_release
title: Summary
date: 2020-02-03
order: 1
toc: true
---

# Beta Release 0.8
## {{ page.title }} - {{ page.date | date: "%D" }}

## Intro
This document describes the new features available in the **0.8-beta release** version of the Virtual Assistant across the following categories:
- Virtual Assistant and Skills
- Clients and Channels

## Virtual Assistant and Skills
### Bot Framework 4.7 Support
{:.no_toc}

The Virtual Assistant and Skill Templates have been updated to use the Bot Builder SDK v4.7 skills implementation. This marks the transition of Skills from Preview into GA as part of the core SDK. Skills are now supported across C#, Javascript/Typescript and Python with Java support to follow. 

Skills are the conversational component model for Bot Framework. Power Virtual Agents now has now added support for Skills through the new manifest schema.

All skills have been updated to the new v4.7 SDK skills implementation and have updated manifest files. The latest botskills CLI tool also provides support for the new manifest format.

For more guidance on how to update your project to use this feature, refer to the following articles:
- [Migrate existing Virtual Assistant to Bot Framework Skills GA]({{site.baseurl}}/overview/whats-new/0.8-beta/migrate-existing-va-to-0.8)
- [Migrate existing Skills to Bot Framework Skills GA]({{site.baseurl}}/overview/whats-new/0.8-beta/migrate-existing-skills-to-0.8)

### QnA Maker Dialog Support
{:.no_toc}

This release updates the Virtual Assistant Template to use the QnAMakerDialog released in Bot Builder SDK v4.6. QnAMakerDialog introduces support for [Follow-Up prompts](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/multiturn-conversation) and [Active learning](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/improve-knowledge-base) along with use of cards for cases of ambiguity. 

For more guidance on how to update your project to use this feature, refer to the following articles:
- [QnA Maker updates]({{site.baseurl}}/overview/whats-new/0.8-beta/qnamaker)

### MainDialog.cs updates
In this release the ActivityHandlerDialog.cs has been deprecated and replaced with a Waterfall Dialog implementation in the MainDialog of the Virtual Assistant and Skill templates. 

For more guidance on how to update your Virtual Assistant to use the updated MainDialog approach, refer to the following article:
- [MainDialog updates]({{site.baseurl}}//overview/whats-new/0.8-beta/maindialog-updates)


### Deployment updates
{:.no_toc}

This release includes a number of updates and improvements to the Virtual Assistant and Skill deployment scripts. These changes include ARM template deployment of LUIS Authoring Keys, migrating to the consolidated Bot Framework CLI, and support for Azure US Government deployments. 

#### LUIS Authoring Resource ARM support
{:.no_toc}

The Virtual Assistant deployment now supports provisioning a LUIS Authoring Resource through ARM deployment. This removes the need to explicitly provide an authoring key during the deployment, and allows for RBAC on the resource enabling authoring keys to be shared by users of the same Azure subscription. 

#### Bot Framework CLI migration
{:.no_toc}

Work has begun on migrating from the deprecated Bot Builder Tools to the consolidated Bot Framework CLI. The QnA Maker, LUDown, and LuisGen commands have been converted in the deployment scripts, with support for LUIS coming in a subsequent release.

#### Azure US Government support
{:.no_toc}

This release enables the Virtual Assistant resources to be deployed in the Azure US Government Cloud. Support for Skills in Azure US Cloud will follow in a subsequent release.

For more guidance on how to deploy your Virtual Assistant to Azure US Government Cloud, refer to the following article:
- [Deploy Virtual Assistant to Azure US Cloud]({{site.baseurl}}//overview/whats-new/0.8-beta/azure-gov-deployment)

## Clients and Channels
### Android Virtual Assistant Client
{:.no_toc}
The Android client for the Virtual Assistant has been updated to support both [custom wake words](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-devices-sdk-create-kws) and authentication using the [Linked Accounts solution]({{site.baseurl}}/solution-accelerators/samples/linked-accounts/). 

## Summary
Microsoft is committed to bringing our customers the ability to bring their own unique Virtual Assistant experiences to their users by providing the tools and control that is required in a world of many Virtual Agents. We look forward to how you take these enhancements forward to enable your customers in the future. As always you can provide feature requests and/or bug reports at [https://github.com/microsoft/botframework-solutions/issues](https://github.com/microsoft/botframework-solutions/issues).

## Release notes
For more information on the changes in this release and version compatibility, please refer to the [v0.8-beta release notes](https://github.com/microsoft/botframework-solutions/releases/tag/v0.8-beta). 
