# Manual Authentication Configuration steps for Productivity Skills (local-mode only or if automatic creation failed)

If you wish to make use of the Calendar, Email and Task Skills standalone to the Virtual Assistant (local mode) you need to configure an Authentication Connection enabling use of your Assistant to authenticate against services such as Office 365 and securely store a token which can be retrieved by your assistant when a user asks a question such as *"What does my day look like today"* to then use against an API like Microsoft Graph.

> These steps are not required if you plan to use the productivity skills as part of the Virtual Assistant, these steps are performed automatically when you add a Skill to your assistant. However, in some envrionments automatic creation isn't possible hence you'll be directed to these manual steps.

The [Add Authentication to your bot](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-authentication?view=azure-bot-service-4.0&tabs=aadv1%2Ccsharp%2Cbot-oauth) section in the Azure Bot Service documentation covers more detail on how to configure Authentication. However in this scenario, the automated deployment step for the Skill has already created the **Azure AD v2 Application** for your Bot and you instead only need to follow these instructions:

- Navigate to the Azure Portal, Click Azure Active Directory and then `App Registrations`
- Find the Application that's been created for your Bot as part of the deployment. You can search for the application by name or ApplicationID as part of the experience but note that search only works across applications currently shown and the one you need may be on a separate page.
- Click API permissions on the left-hand navigation
  - Select Add Permission to show the permissions pane
  - Select `Microsoft Graph`
  - Select Delegated Permissions and then add each of the following permissions required for the Productivity Skills you are adding (see the specific documentation page for the specific scopes required.)
 -  Click Add Permissions at the bottom to apply the changes.

Next you need to create the Authentication Connection for your Bot. Within the Azure Portal, find the `Web App Bot` resource created when your deployed your Bot and choose `Settings`. 

- Scroll down to the oAuth Connection settings section.
- Click `Add Setting`
- Type in the name of your Connection Setting - e.g. `Outlook`
- Choose `Azure Active Directory v2` from the Service Provider drop-down
- Open the `appSettings.config` file for your Skill
    - Copy/Paste the value of `microsoftAppId` into the ClientId setting
    - Copy/Paste the value of `microsoftAppPassword` into the Client Secret setting
    - Set Tenant Id to common
    - Set scopes to match the ones provided in the earlier step.

![Manual Auth Connection](/docs/media/manualauthconnection.png)

Finally, open the  `appSettings.config` file for your Skill and update the connection name to match the one provided in the previous step.

```
"oauthConnections": [
    {
      "name": "Outlook",
      "provider": "Azure Active Directory v2"
    }
  ],
```
