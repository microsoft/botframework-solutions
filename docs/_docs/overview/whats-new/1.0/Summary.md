---
category: Overview
subcategory: What's New
language: 1_0_release
title: Summary
date: 2020-04-21
order: 1
toc: true
---

# Release 1.0 (GA)
## {{ page.title }}

This document describes the new features available in the **1.0 GA release** version of the Virtual Assistant across the following categories:
- Virtual Assistant and Skills

## Virtual Assistant and Skills
### Current State of Feedback
{:.no_toc}
The Feedback Middleware approach has been deprecated since the 0.8 release of **Microsoft.Bot.Solutions**. 
With the 1.0 release we have implemented a temporary feedback mechanism which is outlined [here](https://aka.ms/bfFeedbackDoc). We will have an 
incremental release in the near future with a more robust feedback implementation that will be a part of the **Microsoft.Bot.Solutions** library. 

### Language Generation
{:.no_toc}
With the **1.0 GA release**, we are now utilizing Language Generation 4.9.1 GA. As part of this, there are some breaking changes to LG syntax which may require you to update your .lg files to adhere to new syntax.

For full details regarding these breaking changes, please refer to the [Language Generation 4.8 Preview breaking changes](https://github.com/microsoft/BotBuilder-Samples/tree/master/experimental/language-generation#48-preview).

### Single Sign-On for Skills
{:.no_toc}
In the previous **0.8-beta release**, we added documentation covering how to enable single sign-on for Skills. In the **1.0 GA release**, these changes are officially included.

For instructions on enabling single sign-on for Skills, refer to the instructions located here: [Enable SSO with Skills using OAuthCredentials setting]({{site.baseurl}}//overview/whats-new/1.0/enable-sso-with-skills-using-oauthcredentials-setting).
