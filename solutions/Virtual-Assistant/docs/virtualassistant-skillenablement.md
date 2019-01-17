# Creating a skill using the Skill Template
1. Install vsix
1. add new skill project to VA solution Skills folder

    ![Screenshot](screenshot)

    ![Screenshot](screenshot)
1. Add references to Microsoft.Bot.Solutions to your new skill and test projects
1. Rebuild project
1. Update VA deployment scripts
    - Add references to skill intents in dispatch.lu (all languages)
        ```
        ```
    - bot.recipe - add the skill config (all languages)
        ```
        ```
1. Run deploy_bot.ps1
1. In VA, add reference to your new skill project
1. In VA, add skill config to appsettings.json
    ```
    ```
1. Run luisGen tool to update Dispatch.cs
    ```
    ```
1. Update MainDialog.cs
1. Run VA
1. Test with sample query "sample dialog".

    ![Screenshot](screenshot)

# Customizing your Skill
1. Start by identifying the different tasks your skill will handle
1. Create a luis model
    - Keep your intents discrete and avoid overlap with other skills you'll be adding to your assistant.
1. Create your dialog flows
    - Consider both the local and skill mode experience in your design. A skill should work well in isolation and when included in an assistant solution. When your skill is ready to hand control back to the assistant solution, it must send an EndOfConversation activity. This is handled in the template in MainDialog.CompleteAsync(). 
1. Update the skill configuration in Virtual Assistant appsettings.json
    - **supportedProviders**: this section is for identifying the different authentication providers your skill supports. The value is the "Service Provider" from your OAuth connection in the Azure portal. If your skill does not provide an authenticated experience, leave this section blank.

        ![Screenshot](screenshot)

        ```
            "supportedProviders": [
                "Azure Active Directory v2",
                "Google"
            ]
        ```
    - **luisServiceIds**: this section identifies which LUIS service configurations should be sent from the Virtual Assistant to your skill. Include ids for any LUIS models your skill will need to access in this list. The id for a LUIS service is found in the .bot file configuration.
        ```
            "luisServiceIds": [
                "calendar",
                "general"
            ]
        ```
    - **parameters**: this section is for state values the Virtual Assistant should pass to your skill. For example, the Assistant might have access to the user's location, timezone, and other preferences that tha skill might want to access.
        ```
            "parameters": [
                "IPA.Timezone"
            ]
        ```
    - **configuration**: this section is for any additional key/value configuration the skill may need. For example, if there is a service subscription key the skill needs, this should be supplied through the Virtual Assistant and will be passed to the skill at initialization.
        ```
            "configuration": {
                "AzureMapsKey": ""
            }
        ```