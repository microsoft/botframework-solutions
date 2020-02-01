---
category: Overview
subcategory: What's New
language: 0_8_release
title: achieve SSO among skills
description: explains the steps to take to achieve SSO experience when multiple skills are added to a Virtual Assistant
order: 4
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}

# SSO for skills

In the previous version of Virtual Assistant and Skills, we were able to achieve SSO with the approach of letting Virtual Assistant be the one that acts on behalf of skills to retrieve token and pass it along to the skills. This is not the best approach because it relies on tooling support when adding a skill to a Virtual Assistant. It also isn't the most secure approach especially when the skill is a 3rd party skill. In the 0.8 release, we are starting to use the Skills capabilities from the core BotBuilder SDK and that also changes the model of how a skill retrieves a token. Now skills are the party that retrieves a token directly, and skills would either receive a token directly (when Enhanced Authorization feature in directline is enabled https://blog.botframework.com/2018/09/25/enhanced-direct-line-authentication-features/) or a magic code will show up and the user will send the magic code over to the skill through virtual assistant. In both cases the Virtual Assistant will not get its hand on the token for the skill. But it also means user will be prompted multiple times when switching between skills. This is not a great experience. Fortunately we are going to have the capabilities to achieve this in the upcoming release of BotBuilder SDK R8 (4.8). In this doucment, we will explain how to achieve SSO now by using the preview packages of BotBuilder SDK as well as small minor changes to the skill source code

1. Update your skill to use the latest BotBuilder 4.8.0 preview from myget.
    You should be able to get the latest of the preview packages in this myget feed: https://botbuilder.myget.org/gallery/botbuilder-v4-dotnet-daily. And please also make sure you include this feed in your Visual Studio nuget source configuration.
    Please make sure you update all BotBuilder library to the same preview version
    Please also note that because these are preview packages, they might have some issues since they're not stablized release-ready packages.

2. Update your skill's test project to use the same preview package from the skill project itself to ensure that your solution will compile

3. When you use MultiProviderAuthDialog in any dialogs in your skill, please follow this pattern

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

    As you can see, when creating an OAuthPromptSettings instance, there's a new property called **OAuthAppCredentials**. It's introduced in the 4.8 release that allows you to specify AppCredentials for OAuth that could be different from your skill bot's own AppCredentials. This gives a bot the flexibility of not always having to use the bot's credentials for OAuth, thus enabling multiple skills to be able to use the same AppCredentials for OAuth, to achieve SSO. If you have multiple skills, all you have to do is to make sure you configure all the necessary OAuth Connections in the Bot settings page, and use that same AppCredentials in all your skills that you want to achieve SSO for. This way the user won't have to be prompted again when switching to different skills that are in the same trust boundary.