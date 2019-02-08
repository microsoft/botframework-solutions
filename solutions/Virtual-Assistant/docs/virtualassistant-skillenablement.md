# Creating a skill using the Skill Template
1. Install VSIX from [MyGet](https://botbuilder.myget.org/gallery/aitemplates).
1. Add a new **Skill Template with Tests** project to your solution in the Skills folder. 
    ![Screenshot](./media/skills_addproject.jpg)

    ![Screenshot](./media/skills_addproject2.jpg)

     > NOTE: Your skill must be in the Virtual-Assistant\src\csharp\skills directory to ensure proper resource loading.

    ![Screenshot](./media/skills_projects.jpg)

1. Add references to **Microsoft.Bot.Solutions** to your new skill and test projects
2. Rebuild project to verify there are no errors.
3. Add your Skill LUIS models to the bot.recipe file located within your assistant project: `assistant\DeploymentScripts\en\bot.recipe`

    ```
        {
            "type": "luis",
            "id": "MySkill",
            "name": "MySkill",
            "luPath": "..\\skills\\MySkill\\MySkill\\CognitiveModels\\LUIS\\en\\MySkill.lu"
        }
    ```

4. Add dispatch references to the core LUIS intents for the skill within the `assistant\CognitiveModels\LOCALE\dispatch.lu` file as shown below and repeat for all locales your skill supports. This enables the Dispatcher to understand your new capabilities and route utterances to your skill.
     
    ```
        # l_MySkill 
        - [Sample intent](../../../../skills/MySkill/MySkill/CognitiveModels/LUIS/en/MySkill.lu#Sample)
    ```
5. Run the following script to deploy the new Skill LUIS models and to update the dispatcher, if you omit the locales parameter it will update all languages.
    ```
    PowerShell.exe -ExecutionPolicy Bypass -File DeploymentScripts\update_published_models.ps1 -locales "en-us"
    ```
6. In Virtual Assistant, add a project reference to your new skill project. This tells the Virtual Assistant that there is a new skill available for use.
    ```
       "skills":[
            {
                "type": "skill",
                "id": "MySkill",
                "name": "MySkill",
                "assembly": "MySkill.MySkill, MySkill, Version=1.0.0.0, Culture=neutral",
                "dispatchIntent": "l_MySkill",
                "supportedProviders": [],
                "luisServiceIds": [
                    "MySkill",
                    "general"
                ],
                "parameters": [],
                "configuration": {}
            }
        ]
    ```
7. Run the LuisGen tool to update Dispatch.cs.
    ```
    LUISGen DeploymentScripts\en\dispatch.luis -cs Dispatch -o Dialogs\Shared\Resources 
    ```
8. Update **MainDialog.cs** with the dispatch intent for your skill.
    ![](./media/skills_maindialogupdate.jpg)

9.  Run the Virtual Assistant project.
10. Test your new skill with the query "sample dialog".

    ![Screenshot](./media/skills_testnewskill.jpg)

# Customizing your Skill
1. Start by identifying the different tasks your skill will handle
1. Create a LUIS model
    - Keep your intents discrete and avoid overlap with other skills you'll be adding to your assistant.
1. Create your dialog flows
    - Consider both the local and skill mode experience in your design. A skill should work well in isolation and when included in an assistant solution. 
    - When your skill is ready to hand control back to the assistant solution, it must send an EndOfConversation activity. This is handled in the template in MainDialog.CompleteAsync(). 
1. Update the skill configuration in Virtual Assistant appsettings.json
    - **supportedProviders**: this section is for identifying the different authentication providers your skill supports. If your skill does not provide an authenticated experience, leave this section blank.

        ```
            "supportedProviders": [
                "Azure Active Directory v2",
                "Google"
            ]
        ```
        The value is the "Service Provider" from your OAuth connection in the Azure portal.

        ![Screenshot](./media/skills_oauthprovider.jpg)

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
