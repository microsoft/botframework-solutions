---
category: Skills
subcategory: Handbook
title: Authentication
description: A Skill needs to be able to authenticate the request coming from another bot (Virtual Assistant). The Skill model requires two levels of Authentication
order: 4
toc: true
---

# {{ page.title }}
{:.no_toc}
{{ page.description }}


## JWT Authentication

The Virtual Assistant needs to include an Authorization header in the request. 

This is needed as a Skill needs to verify that the request comes from a properly registered bot service, and that the request was intended for the skill. Because every bot service is a Microsoft app, we can leverage AAD to obtain JWT token as well as verification

![Skill Authentication Flow]({{site.baseurl}}/assets/images/virtualassistant-skillauthentication.png)

Between Virtual Assistant and skill bot, we'll use AAD as the authority to generate and validate token. The token will be a JWT token. Virtual Assistant will use this information to request a JWT token:
  1. Microsoft app id - this will become the source appid claim in the token
  1. Microsoft app password
  1. Skill bot's Microsoft app id - this will become the audience claim in the token

The JWT token will be a 'bearer' token so it'll be part of the Authorization header.

When skill bot receives a request, it looks at the Authorization header to retrieve the token. Then it will rely on AAD to decrypt & validate the JWT token. Right now the skill will only verify if the audience claim is the skill's Microsoft app id. If the audience claim verification passes, then the authentication succeeds and the request moves forward into the skill bot for further processing.

By default, a skill that's created out of a Skill Template enables JWT authentication. 

On the Virtual Assistant side, we use the SkillDialog to dispatch requests to the skills. To enable Virtual Assistant to obtain proper JWT token to send a request to skill, you need to have these lines of code when you create the SkillDialog instances:

```csharp
var credentials = new MicrosoftAppCredentialsEx(settings.MicrosoftAppId, settings.MicrosoftAppPassword, skill.MSAappId);
skillDialogs.Add(new SkillDialog(skill, credentials, telemetryClient, userState, authDialog));
```

The **MicrosoftAppCredentialsEx** class provided within the Microsoft.Bot.Builder.Skills package is the central place to manage the information needed for the skill to obtain the AAD token. Once you pass this into the SkillDialog, the SkillDialog will be able to use it to properly retrieve the AAD token. This behavior is the default behavior if you create a Virtual Assistant out of the Virtual Assistant Template VSIX.

## Whitelist Authentication

After the JWT token is verified, the Skill bot needs to verify if the request comes from a bot that's previously included in a whitelist. A Skill needs to have knowledge of it's callers and give permissions to that bot explicitly instead of any bot that could call the Skill. This level of authorization is enabled by default as well, making sure a Skill is well protected from public access. Developers need to do the following to implement the Whitelist mechanism:

Declare a class **WhiteListAuthProvider** in the bot service project that implements the interface **IWhitelistAuthenticationProvider**

```csharp
public HashSet<string> AppsWhitelist
{
    get
    {
        return new HashSet<string>
        {
            // add AppIds of Virtual Assistant here
        };
    }
}
```

By adding the Microsoft App id of the Virtual Assistant that's calling the Skill into the property AppsWhitelist, you are allowing the bot that's associated with that app id to invoke your skill.

In **Startup.cs**, register a singleton of the interface with this class

```csharp
// Register WhiteListAuthProvider
services.AddSingleton<IWhitelistAuthenticationProvider, WhiteListAuthProvider>();
```

In **BotController.cs** (derived from the **SkillController**, add the class as a new parameter to the constructor

```csharp
public BotController(
    IBot bot,
    BotSettingsBase botSettings,
    IBotFrameworkHttpAdapter botFrameworkHttpAdapter,
    SkillWebSocketAdapter skillWebSocketAdapter,
    IWhitelistAuthenticationProvider whitelistAuthenticationProvider)
    : base(bot, botSettings, botFrameworkHttpAdapter, skillWebSocketAdapter, whitelistAuthenticationProvider)
{}
```

With all these changes in place, you're enabling your Skill to allow bots to invoke it as long as the bot's Microsoft App id is included in the whitelist.

## Token Flow

To ensure a standardized user experience across all Skills, the parent Bot is responsible for managing token requests. This helps to ensure that tokens common across multiple Skills can be shared and the user isn't prompted to authenticate for every Skill.
When a token isn't already cached (e.g. first time use) the following flow occurs:
- When a Skill requests a token, it asks the calling Bot for a token using an event called **tokens/request**
- The Skill starts an EventPRompt waiting for an Event to be returned called **tokens/response**
- The Bot makes use of an OAuthPrompt to surface a prompt to the user
- When a token is retrieved it's returned to the Bot within a **tokens/response** activity, which is used to complete the OAuthPrompt and store the token securely
- The same event is then forwarded to the Skill through the SkillDialog on the stack and provides a token for the Skill to use

![Initial authentication flow for Skills]({{site.baseurl}}/assets/images/virtualassistant-SkillAuthInitialFlow.png)

Subsequent activations benefit from the Azure Bot Service provided cache, which enables silent retrieval of a token.

![Subsequent authentication flow for Skills]({{site.baseurl}}/assets/images/virtualassistant-SkillAuthSubsequentFlow.png)

## Manual authentication

If you wish to make use of the Calendar, Email and Task Skills standalone to the Virtual Assistant (local mode) you need to configure an Authentication Connection enabling use of your Assistant to authenticate against services such as Office 365 and securely store a token which can be retrieved by your assistant when a user asks a question such as *"What does my day look like today"* to then use against an API like Microsoft Graph.

> These steps are not required if you plan to use the productivity skills as part of the Virtual Assistant, these steps are performed automatically when you add a Skill to your assistant.

The [Add Authentication to your bot](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-authentication?view=azure-bot-service-4.0&tabs=aadv1%2Ccsharp%2Cbot-oauth) section in the Azure Bot Service documentation covers more detail on how to configure Authentication. However in this scenario, the automated deployment step for the Skill has already created the **Azure AD v2 Application** for your Bot and you instead only need to follow these instructions:

- Navigate to the Azure Portal, Click Azure Active Directory and then **App Registrations**
- Find the Application that's been created for your Bot as part of the deployment. You can search for the application by name or ApplicationID as part of the experience but note that search only works across applications currently shown and the one you need may be on a separate page.
- Click API permissions on the left-hand navigation
  - Select Add Permission to show the permissions pane
  - Select **Microsoft Graph**
  - Select Delegated Permissions and then add each of the following permissions required for the Productivity Skills you are adding (see the specific documentation page for the specific scopes required.)
 -  Click Add Permissions at the bottom to apply the changes.

Next you need to create the Authentication Connection for your Bot. Within the Azure Portal, find the **Web App Bot** resource created when your deployed your Bot and choose **Settings**. 

- Scroll down to the oAuth Connection settings section.
- Click **Add Setting**
- Type in the name of your Connection Setting - e.g. **Outlook**
    - This name will be displayed to the user in an OAuth card, ensure that it is clear what this maps to
- Choose **Azure Active Directory v2** from the Service Provider drop-down
- Open the **appSettings.config** file for your Skill
    - Copy/Paste the value of **microsoftAppId** into the ClientId setting
    - Copy/Paste the value of **microsoftAppPassword** into the Client Secret setting
    - Set Tenant Id to common
    - Set scopes to match the ones provided in the earlier step.

![Manual Auth Connection]({{site.baseurl}}/assets/images/manualauthconnection.png)

Finally, open the  **appSettings.config** file for your Skill and update the connection name to match the one provided in the previous step.

```
"oauthConnections": [
    {
      "name": "Outlook",
      "provider": "Azure Active Directory v2"
    }
  ]
```