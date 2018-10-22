# Virtual Assistant - Linked Accounts

## Overview

Speech-led conversational scenarios require a different mindset and approach for certain scenarios, one such example is Authentication. If you take a Productivity scenario, whereby the user wants to access information in their calendar it's important for the IPA Bot to have access to a security token (Office 365 for example). 

The first time this scenario executes IPA will need to prompt the user for authentication, normally this is done by returning an OAuthCard to the user along with a Button with underlying link to an OAuth authentication page as shown below.

OAuth Card Example:

![Example OAuth Card](./media/virtualassistant-LinkedAccountsOAuthCard.png)

Signin Page Example

![Example Login Page](./media/virtualassistant-LinkedAccountsSignin.png)

In a speech-led scenario it's not acceptable or practical for a user to enter their username and password through voice commands. Therefore a separate companion experience provides an opportunity for the user to signin-in and provide permission for an IPA Bot to retrieve a token for later use. The Linked Accounts feature of the IPA provides a reference example of a Web-Page using a new set of Azure Bot Service APIs to deliver this feature, you can use this example to build an account linking experience within your own Web-Site or Mobile-App.

## Authentication Configuration

In order to perform Account Linking, the Linked Accounts Web-Site will need the end-user to login using the same account as they'll use to Authenticate as a user of your IPA Bot. for example darren@contosoassistant.com. The ``appsettings.json`` file in the sample project has the following OAuth configuration entry for you to complete, the default example is for a microsoft.online.com scenario.

Update this in-line with your chosen identity provider. If you wish to use login.microsoftonline.com, login to the Azure Portal, Choose Azure Active Directory and create a new Application within App Registrations. Use the ``Application ID`` as your ClientId.

```
"AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "microsoft.onmicrosoft.com",
    "TenantId": "microsoft.com",
    "ClientId": "YOUR_CLIENTID",
    "CallbackPath": "/signin-oidc"
  }
```

Back in the Azure Active Directory configuration page, you will need to set a Reply URL matching the URL for your linked accounts application. 

> Note that the reply URL will need to completely match for sign-in to work. For a local test website this will typically require a signin-oidc suffix. e.g. http://localhost:26923/signin-oidc 

This sample uses the AD Object Identifier claim (``AadObjectidentifierClaim``) as the unique user identifier when performing token operations. This needs to be the same claim used by your IPA Bot when requesting tokens.

The Linked Accounts Web-Site also requires access to the IPA Bot ApplicationId and Secret in order to manage account linking and securely store authentication tokens linked to the end-users identity and the following configuration entries should be updated.

```
"MicrosoftAppId": "YOUR_IPA_BOT_APPLICATIONID",
"MicrosoftAppPassword": "YOUR_IPA_BOT_APPLICATION_SECRET" 
```
  
The final configuration is the DirectLine secret for your IPA Bot, this is required to avoid prompts for magic codes which are otherwise required to protect against man-in-the-middle attacks. Exchanging a DirectLine secret for a Token and providing a TrustedOrigin enables removal of the magic code step.

> Your IPA Bot will need to be deployed and have a Direct-Line channel configured within the Azure portal

```
  "ClientDirectLineSecret": "YOUR_DIRECTLINE_SECRET",
  "ClientDirectLineEndpoint": "https://directline.botframework.com/v3/directline"
```
## Testing Linked Accounts

If you run the project within Visual Studio you will be navigated to the Linked Accounts website. You'll be prompted to login, ensure you choose the same credentials that your IPA Bot will be using when performing operations on your behalf. 

> If different accounts are used then the IPA Bot will not have access to your linked tokens and may prompt for authentication.

Once logged in you, Click Linked Accounts in the top navigation page and you should see a list of the Authentication connections configured for the Bot (whose MicrosoftAppId you specified in the earlier configuration step).

You can now click Sign-In to be navigated to the respective OAuth sign-in page. Once complete the Linked Accounts website should show the updated status.

## Testing your Virtual Assistant with Linked Accounts

Now that you've linked your account and stored tokens you can move back to your Virtual Assistant and check that it's able to use the tokens you've stored and not prompt for authentication.

> Note that when communicating with the Virtual Assistant (Bot) the From.Id property on each Activity must be populated with the same User unique-identifier as this is the *key* used by the Authentication logic to retrieve tokens. The Linked Accounts website sample uses the [objectidentifier](https://docs.microsoft.com/en-us/azure/architecture/multitenant-identity/claims) claim and this must be used by your Virtual Assistant client app or Test Harnesses. Equally, the principal name (upn) could be used if preferred.

Asking a question that triggers a user flow which requires the specified token should now not prompt for authentication.