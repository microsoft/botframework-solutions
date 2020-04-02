---
category: Overview
subcategory: What's New
language: 1_0_release
title: Enable SSO with Skills using OAuthCredentials setting
date: 2020-03-31
order: 1
toc: true
---

# 1.0 Release
## {{ page.title }}
{:.no_toc}
{{ page.description }}

## Intro
 
In the previous release (0.8) we added documentation about [enabling SSO for Skills](https://microsoft.github.io/botframework-solutions/overview/whats-new/0.8-beta/achieve-SSO-among-skills/). In the 1.0 release this is officially included. When a Virtual Assistant has multiple Skills added, in order to achieve SSO, one approach is all the Skills to share the OAuth connection settings from one central Bot Channel Registration, preferrably from the Virtual Assistant. 

1. Add OAuth connection setting that includes all the necessary scopes that are needed for all the Skills
   Go to Virtual Assistant's Azure portal Settings tab, and click 'Add Setting' to add an OAuth connection
   ![Add NuGet Package]({{site.baseurl}}/assets/images/add-nuget.png)

1. In the Skills (take [CalendarSkill](https://github.com/microsoft/botframework-skills/tree/master/skills/csharp/calendarskill) for example), make sure to have oauthCredentials included in appsettings.json

    ```json

    "oauthCredentials": {
        "microsoftAppId": "(Virtual Assistant MsAppId)",
        "microsoftAppPassword": "(Virtual Assistant MsAppPassword)"
    },

    ```

1. In the same appsettings.json file, make sure the oauthConnections setting has the correct OAuth connection name

    ```json

    "oauthConnections": {
        "name": "(Virtual Assistant's OAuth connection name)",
        "provider": "(Virtual Assistant's OAuth connection provider)"
    },

    ```

1. In the skill code that is using MultiProviderAuthDialog class to do OAuth, be sure to use these new settings with this code

    ```csharp

        AppCredentials oauthCredentials = null;
        if (Settings.OAuthCredentials != null &&
            !string.IsNullOrWhiteSpace(Settings.OAuthCredentials.MicrosoftAppId) &&
            !string.IsNullOrWhiteSpace(Settings.OAuthCredentials.MicrosoftAppPassword))
        {
            oauthCredentials = new MicrosoftAppCredentials(Settings.OAuthCredentials.MicrosoftAppId, Settings.OAuthCredentials.MicrosoftAppPassword);
        }

        AddDialog(new MultiProviderAuthDialog(Settings.OAuthConnections, null, oauthCredentials));

    ```

    Note that the Settings property is from BotSettings class which inherits from BotSettingsBase class from Microsoft.Bot.Solutions library. It will automatically pull in the oauthCredentials settings from appsettings.

With these changes, in the Skills that use the same oauthCredentials and OAuth connection, users only have to login once and when users switch to use a different skill, the skill will be able to retrieve the OAuth token without prompting users again. 