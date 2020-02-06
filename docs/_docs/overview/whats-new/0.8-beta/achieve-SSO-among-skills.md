---
category: Overview
subcategory: What's New
language: 0_8_release
date: 2020-02-03
title: Enable SSO support with Skills
description: Explains the steps to take to achieve SSO experience when multiple skills are added to a Virtual Assistant
order: 7
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

# SSO for skills

In the previous version of Virtual Assistant and Skills, we were able to achieve Single-Signon Support (SSO) through the approach of enabling a Virtual Assistant to be the one that acts on behalf of skills to broker token requests as part of a shared trust boundary.

This approach was reliant on additional tooling when adding a skill to a Virtual Assistant and wasn't suitable for scenarios involving 3rd party skills.

In the 0.8 release we transitioned to the GA release of Bot Framework Skills which also changes how a skill retrieves tokens. Skills are now responsible for retrieving their own tokens directly (when [Enhanced Authorization feature](https://blog.botframework.com/2018/09/25/enhanced-direct-line-authentication-features/) in directline is enabled) alternatively a magic code will be provided and the user will send the magic code over to the Skill through the Virtual Assistant. 

In both cases the Virtual Assistant will not participate in the token retrieval process. However, in a secenario where multiple Skills require the same type of token, the user will be prompted multiple times when switching between skills which is not a great user experience.

The upcoming R8 release of the BotBuilder SDK R8 (4.8) will provide native support for this scenario, however this document covers extensions you can make to your Skill enabling this style of SSO today through use of preview BotBuilder SDK packages as well as simple changes to the skill source code.

> Due to the preview nature of these packages this should be used for development and testing purposes only.

1. Update your skill to use the latest BotBuilder 4.8.0 preview from myget.

    - Add this package source to your Visual Studio Nuget package configuration : https://botbuilder.myget.org/gallery/botbuilder-v4-dotnet-daily.
    - Update all BotBuilder package references to the same preview version
  
2. Update your Skill test project to use the same preview packages as above to ensure that your solution will compile

3. When you use MultiProviderAuthDialog in any dialogs in your skill, follow this pattern:

    ```csharp
    var oauthPromptSettings = new List<OAuthPromptSettings>();
    Settings.OAuthConnections.ForEach(
        c => oauthPromptSettings.Add(
            new OAuthPromptSettings
            {
                ConnectionName = c.Name,
                Text = "Login",
                OAuthAppCredentials = new MicrosoftAppCredentials("appId", "password")
            }));

    AddDialog(new MultiProviderAuthDialog(Settings.OAuthConnections, null, oauthPromptSettings));
    ```

    As you can see, when creating an OAuthPromptSettings instance, there's is now a new property called **OAuthAppCredentials**. This has been introduced in the 4.8 preview release and enables you to specify AppCredentials for OAuth that could be different from your skill bot's own AppCredentials.
    
    This provides the flexibility of not always having to use the skills credentials for authentication and secure token storage, thus enabling multiple skills to be able to use the same AppCredentials for OAuth achieving SSO. The top-level Virtual Assistant AppId and Password can be used to create a shared boundary across the assistant and all skills.