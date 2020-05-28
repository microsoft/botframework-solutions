---
category: Skills
subcategory: Handbook
title: OAuth for Skills
description: Overview for achieving OAuth for skills
order: 7
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

## Overview

In azure bot service, a bot can use OAuth to gain access to online resources that require authentication. It's documented here: [Add Authentication to a bot](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-authentication?view=azure-bot-service-4.0&tabs=aadv1%2Ccsharp) and [Bot authentication](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-concept-authentication?view=azure-bot-service-4.0). When your bot is working as a skill, meaning to your users, instead of talking to your bot directly through various channels, they are actually using your skill as a backend service when talking to another bot/virtual assistant, OAuth is also supported. 

## OAuth flow

In a non-skill scenario, a bot goes through these steps to perform OAuth for users:

1. bot configures OAuth connection in Bot Channel Registration page

![OAuth Connection]({{site.baseurl}}/assets/images/OAuthConnection.png)

1. once OAuth connection is configured correctly (Click 'Test Connection' to simulate an OAuth flow), in bot code, use OAuthPrompt (included in Microsoft.Bot.Builder.Dialogs library) to kick start an OAuth flow
![Test OAuth Connection]({{site.baseurl}}/assets/images/OAuthConnection-TestConnection.png)
1. the bot will use OAuthPrompt to retrieve SignInUrl (depending on the channel) and include it inside the OAuthCard that the bot returns to the user
1. the user will see an OAuthCard in the client and click on the Login button to go through an OAuth flow
1. after the OAuth flow is finished (either through magic code or not) the bot will receive the OAuth token needed for the respective resource, and use that to perform subsequent steps

![OAuth Transcript]({{site.baseurl}}/assets/images/OAuth-Transcript.png)

In a skill scenario, the overall flow is the same, with the virtual assistant being transparent, meaning that it would just be passing through the OAuthCard back to user from skill, or magic code back to skill from user. In the case of no magic code, the virtual assistant wouldn't pass through the actual token because that'll be considered unsafe (especially in the scenarios of 3rd party skills). In that case the skill will receive the token directly from Azure Bot Service TokenService. 

In previous versions of Virtual Assistant Template, we used to use the Virtual Assistant as the component that acts on behalf of skills to retrieve token, and pass it along to skills. Compared to the new approach which is that the skill takes care of its own OAuth flow, it has these disadvantages:

1. When Virtual Assistant is acting on behalf of the skill to retrieve token and pass it to the skill, VA always has a copy of the token. This is a security issue, especially when the skill is a 3rd party skill.
1. The skill has to account for skill mode and non-skill mode, because the skill runtime has no knowledge how the user is calling it. When Virtual assistant is getting the token instead of the skill itself, the runtime has to perform differently than when it's being used directly by its user. That inconsistency will create confusion.

## SSO

The latest Azure Bot Service has infrastructure built in to support SSO (Single Sign on). Here are the documentation for it:

1. [Single Sign on overview in Azure Bot Service](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-concept-sso?view=azure-bot-service-4.0)
1. [Add Single Sign on to a bot](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-authentication-sso?view=azure-bot-service-4.0&tabs=csharp%2Ceml)
1. [Identity Providers](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-concept-identity-providers?view=azure-bot-service-4.0&tabs=adv1%2Cga2)


## Troubleshooting

During development we often run into issues when using OAuth. Here's some typical errors we get and how to troubleshoot for them

1. Bad Request
This happens when user clicks on the Login button in the OAuthCard. When this happens, it usually means when creating the OAuthPrompt instance, the connection name is wrong. The connection name needs to be the same as the connection setting in Bot Channel Registration page.
1. API Error when calling the online resources
This usually means the token you get back doesn't have enough permission to perform the tasks you're using the API for. Make sure you configure the correct Scope when you create the OAuth Connection.
